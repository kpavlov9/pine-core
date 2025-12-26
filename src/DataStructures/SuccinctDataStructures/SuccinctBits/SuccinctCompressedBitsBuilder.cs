using System.Diagnostics;
using System.Runtime.CompilerServices;

using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.Bits;

using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctBits;

/// <summary>
/// Enhanced SuccinctCompressedBitsBuilder with incremental rank support.
/// Maintains rank samples during building for efficient rank queries.
/// </summary>
public sealed class SuccinctCompressedBitsBuilder : IBits, IBitsBuilder
{
    public nuint Size => _bits.Size;

    internal static readonly byte BlockSize = NativeBitCountMinusOne;
    internal const nuint SuperBlockFactor = 32;
    internal static readonly nuint SuperBlockSize = BlockSize * SuperBlockFactor;
    internal static readonly byte BitsPerClass = (byte)(Is32BitSystem ? 5 : 6);

    public static readonly nuint[][] ClassCounts = new nuint[BlockSize + 1][];
    internal static readonly uint[] ClassBitOffsets = new uint[BlockSize + 1];
    private static readonly uint MaxBitsPerOffset = 0;

    private readonly BitsBuilder _bits;
    
    // Incremental rank support
    private readonly List<nuint> _rankSamples; // Rank at each super-block boundary
    private nuint _lastSampledSize; // Size when we last updated rank samples
    private bool _rankDirty; // Flag indicating rank needs recomputation

    static SuccinctCompressedBitsBuilder()
    {
        var classCounts = ClassCounts;
        for (var n = 0; n <= BlockSize; n++)
        {
            classCounts[n] = new nuint[n + 2];
            classCounts[n][0] = 1;
        }

        for (var n = 1; n <= BlockSize; n++)
        {
            for (var k = 1; k <= n; k++)
            {
                classCounts[n][k] =
                    classCounts[n - 1][k - 1] + classCounts[n - 1][k];
            }
            classCounts[n][n + 1] = 0;
        }

        for (var n = 0; n <= BlockSize; n++)
        {
            var elementsInTheClass = classCounts[BlockSize][n];
            var bits =
                (uint)MathF.Ceiling(MathF.Log(elementsInTheClass + 1, 2));

            ClassBitOffsets[n] = bits;
            MaxBitsPerOffset = Math.Max(MaxBitsPerOffset, bits);
        }
    }

    private SuccinctCompressedBitsBuilder(BitsBuilder bits)
    {
        _bits = bits;
        _rankSamples = [];
        _lastSampledSize = 0;
        _rankDirty = true;
    }

    public SuccinctCompressedBitsBuilder()
    {
        _bits = new BitsBuilder(0);
        _rankSamples =
        [
            0, // Initial sample
        ];
        _lastSampledSize = 0;
        _rankDirty = false;
    }

    public SuccinctCompressedBitsBuilder(IBitsContainer bits)
    {
        _bits = new BitsBuilder(bits);
        _rankSamples = new List<nuint>();
        _lastSampledSize = 0;
        _rankDirty = true;
    }

    public SuccinctCompressedBitsBuilder(nuint initialSize)
    {
        _bits = new BitsBuilder(initialSize);
        _rankSamples = [0];
        _lastSampledSize = 0;
        _rankDirty = false;
    }

    public SuccinctCompressedBitsBuilder(IReadOnlyList<byte> bytes)
    {
        _bits = new BitsBuilder(bytes);
        _rankSamples = [];
        _lastSampledSize = 0;
        _rankDirty = true;
    }

    public SuccinctCompressedBitsBuilder(byte[] bytes)
    {
        _bits = new BitsBuilder(bytes);
        _rankSamples = [];
        _lastSampledSize = 0;
        _rankDirty = true;
    }

    public void Clear()
    {
        _bits.Clear();
        _rankSamples.Clear();
        _rankSamples.Add(0);
        _lastSampledSize = 0;
        _rankDirty = false;
    }

    public void ClearAndInitialize(IBitsContainer bits)
    {
        _bits.ClearAndInitialize(bits);
        _rankSamples.Clear();
        _lastSampledSize = 0;
        _rankDirty = true;
    }

    public void PushZeroes(int bitsCount)
    {
        _bits.AddUnsetBits(bitsCount);
        UpdateRankSamplesIncremental();
    }

    public void PushOnes(int bitsCount)
    {
        _bits.AddSetBits(bitsCount);
        UpdateRankSamplesIncremental();
    }

    public void Add(bool bitValue)
    {
        _bits.Add(bitValue);
        UpdateRankSamplesIncremental();
    }

    public void Set(nuint position)
    {
        _bits.Set(position);
        // Setting a bit in the middle requires recomputation
        if (position < _lastSampledSize)
        {
            _rankDirty = true;
        }
    }

    public void Unset(nuint position)
    {
        _bits.Unset(position);
        // Unsetting a bit in the middle requires recomputation
        if (position < _lastSampledSize)
        {
            _rankDirty = true;
        }
    }

    public bool GetBit(nuint position)
        => _bits.GetBit(position);

    /// <summary>
    /// Updates rank samples incrementally for append operations.
    /// Only processes new bits since last sample update.
    /// </summary>
    private void UpdateRankSamplesIncremental()
    {
        nuint currentSize = _bits.Size;
        
        // Determine how many super-block boundaries we need samples for
        // We need a sample at position 0, SuperBlockSize, 2*SuperBlockSize, etc.
        // up to (but not beyond) the current size
        nuint neededSamples = (currentSize / SuperBlockSize) + 1;
        
        if (neededSamples <= (nuint)_rankSamples.Count)
        {
            // We already have enough samples
            return;
        }

        // If we have no samples yet, start with sample 0
        if (_rankSamples.Count == 0)
        {
            _rankSamples.Add(0);
        }

        // Count from where we left off
        nuint lastSampledPosition = ((nuint)_rankSamples.Count - 1) * SuperBlockSize;
        nuint rank = _rankSamples[_rankSamples.Count - 1];
        
        // Add samples for each new super-block boundary
        for (nuint sampleIdx = (nuint)_rankSamples.Count; sampleIdx < neededSamples; sampleIdx++)
        {
            nuint targetPosition = sampleIdx * SuperBlockSize;
            
            // Count from last position to target (or end of array, whichever comes first)
            nuint endPosition = Math.Min(targetPosition, currentSize);
            
            // Explicit bounds check in loop
            for (nuint i = lastSampledPosition; i < endPosition && i < _bits.Size; i++)
            {
                if (_bits.GetBit(i))
                {
                    rank++;
                }
            }
            
            _rankSamples.Add(rank);
            lastSampledPosition = targetPosition;
        }

        _lastSampledSize = currentSize;
    }

    /// <summary>
    /// Efficiently computes rank using cached samples.
    /// O(SuperBlockSize) worst case = O(2016).
    /// Rank sample at index i represents rank at position (i * SuperBlockSize).
    /// </summary>
    public nuint RankSetBits(nuint position)
    {
        if (position == 0) return 0;
        
        // Clip position to actual size
        if (position > _bits.Size) 
            position = _bits.Size;

        // Ensure rank samples are up to date
        if (_rankDirty)
        {
            RebuildRankSamples();
        }
        else
        {
            UpdateRankSamplesIncremental();
        }

        // Handle empty case
        if (_rankSamples.Count == 0 || _bits.Size == 0)
        {
            return 0;
        }

        // Find which super-block boundary to start from
        nuint sampleIndex = position / SuperBlockSize;
        
        // Get rank at the super-block boundary (or closest available sample)
        nuint rank;
        nuint countFrom;
        
        if (sampleIndex < (nuint)_rankSamples.Count)
        {
            // We have a sample at or before this position
            rank = _rankSamples[(int)sampleIndex];
            countFrom = sampleIndex * SuperBlockSize;
        }
        else
        {
            // Position is beyond our samples - use last sample
            int lastSampleIdx = _rankSamples.Count - 1;
            rank = _rankSamples[lastSampleIdx];
            countFrom = ((nuint)lastSampleIdx) * SuperBlockSize;
        }

        // Ensure countFrom is valid and doesn't exceed position
        if (countFrom > _bits.Size)
        {
            countFrom = _bits.Size;
        }
        if (countFrom > position)
        {
            countFrom = position;
        }

        // Count from super-block boundary to position
        // Explicit bounds check - ensure i < _bits.Size
        nuint size = _bits.Size; // Cache to avoid repeated access
        for (nuint i = countFrom; i < position && i < size; i++)
        {
            if (_bits.GetBit(i))
            {
                rank++;
            }
        }

        return rank;
    }

    /// <summary>
    /// Rank for unset bits.
    /// </summary>
    public nuint RankUnsetBits(nuint position)
    {
        return position - RankSetBits(position);
    }

    /// <summary>
    /// Rebuilds rank samples from scratch. Called when bits are modified in the middle.
    /// Rank sample at index i = number of set bits before position (i * SuperBlockSize).
    /// </summary>
    private void RebuildRankSamples()
    {
        _rankSamples.Clear();
        _rankSamples.Add(0); // Sample 0: rank at position 0 is always 0

        if (_bits.Size == 0)
        {
            _lastSampledSize = 0;
            _rankDirty = false;
            return;
        }

        nuint rank = 0;
        nuint nextSamplePosition = SuperBlockSize;
        nuint size = _bits.Size; // Cache size to avoid repeated property access
        
        // Explicit check i < size to prevent out of bounds
        for (nuint i = 0; i < size; i++)
        {
            if (_bits.GetBit(i))
            {
                rank++;
            }
            
            // Add sample when we reach a super-block boundary
            // Check i+1 because sample represents rank BEFORE the boundary position
            if (i + 1 == nextSamplePosition && nextSamplePosition <= size)
            {
                _rankSamples.Add(rank);
                nextSamplePosition += SuperBlockSize;
            }
        }

        _lastSampledSize = size;
        _rankDirty = false;
    }

    private nuint GetBlocksCount() =>
        (_bits.Size + BlockSize - 1) / BlockSize;
    
    private nuint GetSuperBlocksCount() =>
        (_bits.Size + SuperBlockSize - 1) / SuperBlockSize;

    public static nuint OffsetOf(nuint block, uint @class)
    {
        if (@class == 0 || @class == BlockSize)
        {
            return 0;
        }

        nuint offset = 0;
        for (var i = BlockSize - 1; i > 0; i--)
        {
            if ((block >> i & 1) == 1)
            {
                offset += ClassCounts[i][@class];
                --@class;
            }
        }
        return offset;
    }

    private void InitializeBuilders(
        int size,
        nuint blocksCount,
        out BitsBuilder classValuesBuilder,
        out BitsBuilder offsetValuesBuilder,
        out QuasiSuccinctBitsBuilder rankSamplesBuilder,
        out QuasiSuccinctBitsBuilder offsetPositionSamplesBuilder)
    {
        var superBlocksCount = (int)GetSuperBlocksCount();
        classValuesBuilder = new(BitsPerClass * blocksCount);
        offsetValuesBuilder = new(MaxBitsPerOffset * blocksCount);
        rankSamplesBuilder = new QuasiSuccinctBitsBuilder(
            superBlocksCount,
            size);
        offsetPositionSamplesBuilder = new QuasiSuccinctBitsBuilder(
            superBlocksCount,
            size);
    }

    private static void SplitIntoBlocks(
        nuint blocksCount,
        in Bits.Bits bits,
        BitsBuilder classValuesBuilder,
        BitsBuilder offsetValuesBuilder,
        QuasiSuccinctBitsBuilder rankSamplesBuilder,
        QuasiSuccinctBitsBuilder offsetPositionSamplesBuilder,
        out nuint rankSum)
    {
        rankSum = 0;

        for (nuint i = 0; i < blocksCount; i++)
        {
            if (i % SuperBlockFactor == 0)
            {
                offsetPositionSamplesBuilder.Add(offsetValuesBuilder.Size);
                rankSamplesBuilder.Add(rankSum);
            }

            var position = i * BlockSize;
            var block = bits.FetchBits(position, BlockSize);
            var @class = PopCount(block);
            classValuesBuilder.Add(@class, BitsPerClass);

            var offset = OffsetOf(block, @class);
            offsetValuesBuilder.AddBits(offset, (int)ClassBitOffsets[@class]);

            rankSum += @class;
        }
    }

    private static void CheckEncoding(
        nuint blocksCount,
        in Bits.Bits bits,
        in Bits.Bits offsetValues,
        in Bits.Bits classValues,
        in QuasiSuccinctIndices offsetPositionSamples)
    {
        for (nuint i = 0; i < blocksCount; i++)
        {
            var position = i * BlockSize;
            var block = bits.FetchBits(position, BlockSize);

            var @class = PopCount(block);
            var offset = OffsetOf(block, @class);

            var class2 = SuccinctCompressedBits.ClassOfBlock(i, classValues);
            var offset2 = SuccinctCompressedBits.OffsetOfBlock(
                i, @class, offsetPositionSamples, offsetValues, classValues);
            var block2 = SuccinctCompressedBits.FetchBlock(
                i, @class, offsetPositionSamples, offsetValues, classValues);

            Debug.Assert(@class == class2, $"class: {@class} != {class2} at block {i};");
            Debug.Assert(offset == offset2, $"offset: {offset} != {offset2} at block {i};");
            Debug.Assert(block == block2, $"block: {Convert.ToString((long)block, 2)} != {Convert.ToString((long)block2, 2)} at block {i};");
        }
    }

    private static SuccinctCompressedBits BuildSuccinctBits(
        int size,
        in Bits.Bits bits,
        nuint blocksCount,
        BitsBuilder classValuesBuilder,
        BitsBuilder offsetValuesBuilder,
        QuasiSuccinctBitsBuilder rankSamplesBuilder,
        QuasiSuccinctBitsBuilder offsetPositionSamplesBuilder,
        nuint rankSum)
    {
        var rankSamples = rankSamplesBuilder.Build();
        var offsetPositionSamples = offsetPositionSamplesBuilder.Build();
        var offsetValues = offsetValuesBuilder.Build();
        var classValues = classValuesBuilder.Build();
#if DEBUG
        CheckEncoding(
            blocksCount: blocksCount,
            bits: bits,
            offsetValues: offsetValues,
            classValues: classValues,
            offsetPositionSamples: offsetPositionSamples);
#endif
        return new SuccinctCompressedBits(
            size: (nuint)size,
            setBitsCount: rankSum,
            rankSamples: rankSamples,
            offsetPositionSamples: offsetPositionSamples,
            classValues: classValues,
            offsetValues: offsetValues);
    }

    public SuccinctCompressedBits Build()
    {
        var bits = _bits.Build();
        int size = (int)bits.Size;

        if (size == 0)
        {
            return default;
        }

        var blocksCount = GetBlocksCount();

        InitializeBuilders(
            size,
            blocksCount,
            out var classValuesBuilder,
            out var offsetValuesBuilder,
            out var rankSamplesBuilder,
            out var offsetPositionSamplesBuilder);

        SplitIntoBlocks(
            blocksCount: blocksCount,
            bits: bits,
            classValuesBuilder: classValuesBuilder,
            offsetValuesBuilder: offsetValuesBuilder,
            rankSamplesBuilder: rankSamplesBuilder,
            offsetPositionSamplesBuilder: offsetPositionSamplesBuilder,
            out var rankSum);

        return BuildSuccinctBits(
            size: size,
            bits: bits,
            blocksCount: blocksCount,
            classValuesBuilder: classValuesBuilder,
            offsetValuesBuilder: offsetValuesBuilder,
            rankSamplesBuilder: rankSamplesBuilder,
            offsetPositionSamplesBuilder: offsetPositionSamplesBuilder,
            rankSum: rankSum);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBits BuildBits() => Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISuccinctBits BuildSuccinctBits() => Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISuccinctCompressedBits BuildSuccinctCompressedBits() => Build();

    public IBits ClearAndBuildBits(IBitsContainer bits)
    {
        ClearAndInitialize(bits);
        return Build();
    }

    public ISuccinctBits ClearAndBuildSuccinctBits(IBitsContainer bits)
    {
        ClearAndInitialize(bits);
        return Build();
    }

    public ISuccinctCompressedBits ClearAndBuildSuccinctCompressedBits(IBitsContainer bits)
    {
        ClearAndInitialize(bits);
        return Build();
    }

    public IBitsBuilder Clone() => new SuccinctCompressedBitsBuilder((BitsBuilder)_bits.Clone());
}