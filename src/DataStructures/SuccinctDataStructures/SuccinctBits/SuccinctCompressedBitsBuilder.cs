using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.Bits;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.SuccinctOps;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctBits;

/// <summary>
/// Enhanced SuccinctCompressedBitsBuilder with incremental rank support.
/// Maintains rank samples during building for efficient rank queries.
/// </summary>
public sealed class SuccinctCompressedBitsBuilder : IBits, IBitsBuilder
{
    public nuint Size => _bits.Size;

    private readonly BitsBuilder _bits;
    
    // Incremental rank support
    private readonly List<nuint> _rankSamples;
    private nuint _lastSampledSize;
    private bool _rankDirty;

    // Expose constants for external use
    public static byte BlockSizeValue => BlockSize;
    public static nuint SuperBlockFactorValue => SuperBlockFactor;
    public static nuint SuperBlockSizeValue => SuperBlockSize;
    public static byte BitsPerClassValue => BitsPerClass;
    public static nuint[][] ClassCountsCompat => ConvertToJagged(); // For compatibility
    
    private static nuint[][] ConvertToJagged()
    {
        var result = new nuint[BlockSize + 1][];
        for (int n = 0; n <= BlockSize; n++)
        {
            result[n] = new nuint[n + 2];
            for (int k = 0; k <= n + 1; k++)
            {
                result[n][k] = GetClassCountFlat(n, k);
            }
        }
        return result;
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
        _rankSamples = [0];
        _lastSampledSize = 0;
        _rankDirty = false;
    }

    public SuccinctCompressedBitsBuilder(IBitsContainer bits)
    {
        _bits = new BitsBuilder(bits);
        _rankSamples = [];
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
        // Zeroes don't affect rank, but we need to track size changes
        UpdateRankSamplesLazy();
    }

    public void PushOnes(int bitsCount)
    {
        _bits.AddSetBits(bitsCount);
        UpdateRankSamplesLazy();
    }

    public void Add(bool bitValue)
    {
        _bits.Add(bitValue);
        UpdateRankSamplesLazy();
    }

    public void Set(nuint position)
    {
        _bits.Set(position);
        if (position < _lastSampledSize)
        {
            _rankDirty = true;
        }
    }

    public void Unset(nuint position)
    {
        _bits.Unset(position);
        if (position < _lastSampledSize)
        {
            _rankDirty = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBit(nuint position) => _bits.GetBit(position);

    /// <summary>
    /// Lazily mark that samples may need updating.
    /// Actual computation is deferred until needed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateRankSamplesLazy()
    {
        // Don't compute immediately - just track that we might need more samples
        // This is OPTIMIZATION #14: Lazy rank sample building
    }

    // =========================================================================
    // Rank Operations - Match Original Implementation for Correctness
    // =========================================================================
    // The original uses bit-by-bit counting with GetBit which is guaranteed
    // correct for MSB-first bit ordering. We keep this approach for reliability.

    /// <summary>
    /// Efficiently compute rank using cached samples.
    /// Matches original SuccinctCompressedBitsBuilder implementation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint RankSetBits(nuint position)
    {
        if (position == 0) return 0;
        
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

        if (_rankSamples.Count == 0 || _bits.Size == 0)
            return 0;

        // Find super-block containing position
        nuint sampleIndex = position / SuperBlockSize;
        
        nuint rank;
        nuint countFrom;
        
        if (sampleIndex < (nuint)_rankSamples.Count)
        {
            rank = _rankSamples[(int)sampleIndex];
            countFrom = sampleIndex * SuperBlockSize;
        }
        else
        {
            int lastSampleIdx = _rankSamples.Count - 1;
            rank = _rankSamples[lastSampleIdx];
            countFrom = ((nuint)lastSampleIdx) * SuperBlockSize;
        }

        // Ensure countFrom is valid
        if (countFrom > _bits.Size)
            countFrom = _bits.Size;
        if (countFrom > position)
            countFrom = position;

        // Count remaining bits using GetBit (guaranteed correct for MSB-first ordering)
        nuint size = _bits.Size;
        for (nuint i = countFrom; i < position && i < size; i++)
        {
            if (_bits.GetBit(i))
                rank++;
        }
        
        return rank;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint RankUnsetBits(nuint position)
    {
        return position - RankSetBits(position);
    }

    /// <summary>
    /// Incrementally update rank samples for new bits added at the end.
    /// </summary>
    private void UpdateRankSamplesIncremental()
    {
        nuint currentSize = _bits.Size;
        nuint neededSamples = (currentSize / SuperBlockSize) + 1;
        
        if (neededSamples <= (nuint)_rankSamples.Count)
            return;

        if (_rankSamples.Count == 0)
            _rankSamples.Add(0);

        // Count from where we left off
        nuint lastSampledPosition = ((nuint)_rankSamples.Count - 1) * SuperBlockSize;
        nuint rank = _rankSamples[_rankSamples.Count - 1];
        
        // Add samples for each new super-block boundary
        for (nuint sampleIdx = (nuint)_rankSamples.Count; sampleIdx < neededSamples; sampleIdx++)
        {
            nuint targetPosition = sampleIdx * SuperBlockSize;
            nuint endPosition = Math.Min(targetPosition, currentSize);
            
            // Count bits using GetBit (guaranteed correct)
            for (nuint i = lastSampledPosition; i < endPosition && i < _bits.Size; i++)
            {
                if (_bits.GetBit(i))
                    rank++;
            }
            
            _rankSamples.Add(rank);
            lastSampledPosition = targetPosition;
        }

        _lastSampledSize = currentSize;
    }

    /// <summary>
    /// Rebuild all rank samples from scratch.
    /// Sample[i] = number of set bits before position (i * SuperBlockSize).
    /// </summary>
    private void RebuildRankSamples()
    {
        _rankSamples.Clear();
        _rankSamples.Add(0); // Sample 0: rank before position 0 is always 0

        if (_bits.Size == 0)
        {
            _lastSampledSize = 0;
            _rankDirty = false;
            return;
        }

        nuint rank = 0;
        nuint nextSamplePosition = SuperBlockSize;
        nuint size = _bits.Size;
        
        for (nuint i = 0; i < size; i++)
        {
            if (_bits.GetBit(i))
                rank++;
            
            // Add sample when we reach a super-block boundary
            if (i + 1 == nextSamplePosition && nextSamplePosition <= size)
            {
                _rankSamples.Add(rank);
                nextSamplePosition += SuperBlockSize;
            }
        }

        _lastSampledSize = size;
        _rankDirty = false;
    }

    // =========================================================================
    // Build Methods
    // =========================================================================

    private nuint GetBlocksCount() =>
        (_bits.Size + BlockSize - 1) / BlockSize;
    
    private nuint GetSuperBlocksCount() =>
        (_bits.Size + SuperBlockSize - 1) / SuperBlockSize;

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
        rankSamplesBuilder = new QuasiSuccinctBitsBuilder(superBlocksCount, size);
        offsetPositionSamplesBuilder = new QuasiSuccinctBitsBuilder(superBlocksCount, size);
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

            // Use optimized OffsetOf
            var offset = OffsetOf(block, @class);
            offsetValuesBuilder.AddBits(offset, (int)ClassBitOffsets[@class]);

            rankSum += @class;
        }
    }

    /// <summary>
    /// Compute offset of a block within its class.
    /// Uses flattened ClassCounts for better cache performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint OffsetOf(nuint block, uint @class)
    {
        if (@class == 0 || @class == BlockSize)
            return 0;
        
        nuint offset = 0;
        var blockSize = BlockSize;
        
        for (int i = blockSize - 1; i > 0 && @class > 0; i--)
        {
            if (((block >> i) & 1) == 1)
            {
                offset += GetClassCountFlat(i, (int)@class);
                @class--;
            }
        }
        
        return offset;
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

        var rankSamples = rankSamplesBuilder.Build();
        var offsetPositionSamples = offsetPositionSamplesBuilder.Build();
        var offsetValues = offsetValuesBuilder.Build();
        var classValues = classValuesBuilder.Build();

        return new SuccinctCompressedBits(
            size: (nuint)size,
            setBitsCount: rankSum,
            rankSamples: rankSamples,
            offsetPositionSamples: offsetPositionSamples,
            classValues: classValues,
            offsetValues: offsetValues);
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