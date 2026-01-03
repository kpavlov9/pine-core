using System.Diagnostics;
using System.Runtime.CompilerServices;

using KGIntelligence.PineCore.Helpers.Utilities;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.SuccinctOps;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.Bits;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctBits;


/// <summary>
/// Compressed bit sequence offering rank and select queries with the need of decopression.
/// The paper which inspires the implementation is
/// "Succinct Indexable Dictionaries with Applications to Encoding k-ary Trees and Multisets"
/// by Rajeev Raman, V.Raman and S.Srinivasa Rao: http://arxiv.org/pdf/0705.0552.pdf
///
/// The abbreviation 'RRR' comes from the first letters of the familiy names of 3 authors of the paper.
/// </summary>
public readonly struct SuccinctCompressedBits : ISerializableBits<SuccinctCompressedBits>, ISuccinctCompressedBits
{
    private readonly Bits.Bits _classValues;
    private readonly Bits.Bits _offsetValues;
    private readonly nuint _size;
    private readonly nuint _setBitsCount;
    private readonly QuasiSuccinctIndices _rankSamples;
    private readonly QuasiSuccinctIndices _offsetPositionSamples;

    public nuint Size => _size;
    public nuint SetBitsCount => _setBitsCount;
    public nuint UnsetBitsCount => _size - _setBitsCount;

    public SuccinctCompressedBits(
        nuint size,
        nuint setBitsCount,
        in QuasiSuccinctIndices rankSamples,
        in QuasiSuccinctIndices offsetPositionSamples,
        in Bits.Bits classValues,
        in Bits.Bits offsetValues)
    {
        _rankSamples = rankSamples;
        _offsetPositionSamples = offsetPositionSamples;
        _size = size;
        _setBitsCount = setBitsCount;
        _classValues = classValues;
        _offsetValues = offsetValues;
    }

    // =========================================================================
    // OPTIMIZATION #1 & #6: GetBit with LUT and inlining
    // =========================================================================
    
    /// <summary>
    /// Get bit at position. Optimized with:
    /// - Early exit for all-0 and all-1 blocks
    /// - InverseOffset LUT for O(1) block decoding
    /// - Branch-free bit extraction
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBit(nuint position)
    {
        // Calculate block position using integer division
        // Note: BlockSize is 63 (not power of 2), so we can't use bit shift
        nuint blockPosition = position / BlockSize;
        
        // Get class (popcount) of the block
        var @class = ClassOfBlockFast(blockPosition);
        
        // Fast path: all-0 block
        if (@class == 0)
            return false;
        
        // Fast path: all-1 block  
        if (@class == BlockSize)
            return true;
        
        // Decode block using optimized InverseOffset (uses LUT when possible)
        var block = FetchBlockOptimized(blockPosition, @class);
        
        // Extract bit - branch-free
        int offsetInBlock = (int)(position % BlockSize);
        return ((block >> (BlockSize - 1 - offsetInBlock)) & 1) != 0;
    }

    /// <summary>
    /// Optimized ClassOfBlock with AggressiveInlining.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint ClassOfBlockFast(nuint blockPosition)
    {
        return unchecked((uint)_classValues.FetchBits(
            blockPosition * BitsPerClass,
            BitsPerClass));
    }

    /// <summary>
    /// Static version for use from builder.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ClassOfBlock(nuint blockPosition, in Bits.Bits classValues)
    {
        return unchecked((uint)classValues.FetchBits(
            blockPosition * BitsPerClass,
            BitsPerClass));
    }

    /// <summary>
    /// Fetch and decode a block using optimized InverseOffset with LUT.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private nuint FetchBlockOptimized(nuint blockPosition, uint @class)
    {
        var offset = OffsetOfBlockFast(blockPosition, @class);
        return InverseOffsetOf(offset, @class);
    }

    /// <summary>
    /// InverseOffsetOf using lookup table when available.
    /// Falls back to computation for large classes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint InverseOffsetOf(nuint offset, uint @class)
    {
        // Fast path: class 0 always returns 0
        if (@class == 0)
            return 0;
        
        // Fast path: class == BlockSize means all bits set
        if (@class == BlockSize)
            return (NUIntOne << BlockSize) - 1;
        
        // Try lookup table
        if (@class < (uint)HasInverseOffsetTable.Length && 
            HasInverseOffsetTable[@class] &&
            offset < (nuint)InverseOffsetTables[@class]!.Length)
        {
            return InverseOffsetTables[@class]![offset];
        }
        
        // Fall back to computation
        return ComputeInverseOffset(offset, @class, BlockSize);
    }

    /// <summary>
    /// Internal FetchBlock for static access.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint FetchBlock(
        nuint blockPosition,
        uint @class,
        in QuasiSuccinctIndices offsetPosSamples,
        in Bits.Bits offsetValues,
        in Bits.Bits classValues)
    {
        var offset = OffsetOfBlock(blockPosition, @class, offsetPosSamples, offsetValues, classValues);
        return InverseOffsetOf(offset, @class);
    }

    /// <summary>
    /// Get the offset of a block within its class (member version).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private nuint OffsetOfBlockFast(nuint blockPosition, uint @class)
    {
        var offsetCount = (int)ClassBitOffsets[@class];
        
        var superBlockIndex = blockPosition / SuperBlockFactor;
        var offsetPosition = _offsetPositionSamples.Get(superBlockIndex);
        var j = superBlockIndex * SuperBlockFactor;

        // Accumulate offset positions for blocks before this one
        while (j < blockPosition)
        {
            var jthClass = ClassOfBlockFast(j);
            offsetPosition += ClassBitOffsets[jthClass];
            j++;
        }

        return _offsetValues.FetchBits(offsetPosition, offsetCount);
    }

    /// <summary>
    /// Static version for external use.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint OffsetOfBlock(
        nuint blockPosition,
        uint @class,
        in QuasiSuccinctIndices offsetPosSamples,
        in Bits.Bits offsetValues,
        in Bits.Bits classValues)
    {
        var offsetCount = (int)ClassBitOffsets[@class];

        var superBlockIndex = blockPosition / SuperBlockFactor;
        var offsetPosition = offsetPosSamples.Get(superBlockIndex);
        var j = superBlockIndex * SuperBlockFactor;

        while (j < blockPosition)
        {
            var jthClass = ClassOfBlock(j, classValues);
            offsetPosition += ClassBitOffsets[jthClass];
            j++;
        }

        return offsetValues.FetchBits(offsetPosition, offsetCount);
    }

    private nuint FetchBlock(nuint blockPosition)
    {
        uint @class = ClassOfBlockFast(blockPosition);
        return InverseOffsetOf(
            OffsetOfBlockFast(blockPosition, @class), 
            @class);
    }

    // =========================================================================
    // Rank Operations - Optimized
    // =========================================================================

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint RankUnsetBits(nuint bitPositionCutoff) => 
        bitPositionCutoff - RankSetBits(bitPositionCutoff);

    /// <summary>
    /// Count set bits before position. Optimized with:
    /// - Early exit for all-0 and all-1 super-blocks
    /// - Efficient class accumulation
    /// - Optimized block decoding
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint RankSetBits(nuint bitPositionCutoff)
    {
        var blockSize = BlockSize;
        nuint blockIndex = bitPositionCutoff / blockSize;
        var superBlockFactor = SuperBlockFactor;
        var superBlockSize = SuperBlockSize;

        nuint superBlockIndex = blockIndex / superBlockFactor;
        nuint rank = _rankSamples.Get(superBlockIndex);
        
        // Check for early exit with next sample
        if (superBlockIndex + 1 < _rankSamples.Size)
        {
            nuint rankNext = _rankSamples.Get(superBlockIndex + 1);
            nuint delta = rankNext - rank;
            
            if (delta == 0)
            {
                // All bits in super-block are 0
                return rank;
            }
            
            if (delta == superBlockSize)
            {
                // All bits in super-block are 1
                nuint superBlockHead = superBlockIndex * superBlockSize;
                return rank + (bitPositionCutoff - superBlockHead);
            }
        }

        // Accumulate class counts for blocks before target
        nuint j = superBlockIndex * superBlockFactor;
        while (j < blockIndex)
        {
            rank += ClassOfBlockFast(j);
            j++;
        }

        // Handle partial block
        var block = FetchBlock(blockIndex);
        int posInTheBlock = (int)(bitPositionCutoff % blockSize);
        
        // Count bits in partial block using mask
        nuint mask = ~((NUIntOne << (blockSize - posInTheBlock)) - 1);
        return rank + PopCount(block & mask);
    }

    // =========================================================================
    // Select Operations - Optimized
    // =========================================================================

    /// <summary>
    /// Find position of n-th set bit. Optimized with:
    /// - Binary search over rank samples
    /// - Early exit for all-1 super-blocks
    /// - Optimized block scanning
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint SelectSetBits(nuint bitCountCutoff)
    {
        if (bitCountCutoff >= SetBitsCount)
            return _size;

        // Binary search to find containing super-block
        nuint left = 0;
        nuint right = _rankSamples.Size - 1;

        while (left < right)
        {
            nuint pivot = (left + right) >> 1;
            nuint rankAtPivot = _rankSamples.Get(pivot);
            
            if (bitCountCutoff < rankAtPivot)
                right = pivot;
            else
                left = pivot + 1;
        }
        
        if (right != 0)
            right--;

        var blockPosition = right * SuperBlockFactor;
        var blockSize = BlockSize;
        var rank = _rankSamples.Get(right);

        // Check for all-1s super-block optimization
        if (right + 1 < _rankSamples.Size)
        {
            var delta = _rankSamples.Get(right + 1) - rank;
            if (delta == SuperBlockSize)
            {
                // All bits in super-block are 1
                return blockPosition * blockSize + (bitCountCutoff - rank);
            }
        }

        nuint blocksCount = (_size + blockSize - 1) / blockSize;
        bitCountCutoff -= rank;

        // Linear scan through blocks
        while (blockPosition < blocksCount)
        {
            uint @class = ClassOfBlockFast(blockPosition);

            if (bitCountCutoff < @class)
                break;
            
            blockPosition++;
            bitCountCutoff -= @class;
        }
        
        // Find bit within block
        nuint block = FetchBlock(blockPosition);
        nuint bitAlignedBlock = block << (NativeBitCount - blockSize);
        return blockPosition * blockSize + Select(bitAlignedBlock, (int)bitCountCutoff);
    }

    /// <summary>
    /// Find position of n-th unset bit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint SelectUnsetBits(nuint position)
    {
        if (position >= UnsetBitsCount)
            return _size;

        var superBlockSize = SuperBlockSize;
        var blockSize = BlockSize;

        // Binary search
        nuint left = 0;
        nuint right = _rankSamples.Size - 1;
        
        while (left < right)
        {
            nuint pivot = (left + right) >> 1;
            nuint rankAtPivot = pivot * superBlockSize - _rankSamples.Get(pivot);

            if (position < rankAtPivot)
                right = pivot;
            else
                left = pivot + 1;
        }
        right--;

        nuint j = right * SuperBlockFactor;
        nuint rank = _rankSamples.Get(right);
        nuint delta = _rankSamples.Get(right + 1) - rank;

        if (delta == 0)
        {
            // All bits in super-block are 0
            return j * blockSize + (position - (rank + 1));
        }

        position -= right * superBlockSize - rank;

        // Linear scan through blocks
        while (true)
        {
            uint c = ClassOfBlockFast(j);
            uint r = blockSize - c;
            
            if (position < r)
                break;
            
            j++;
            position -= r;
        }

        nuint block = FetchBlock(j);
        nuint bitAlignedBlock = block << (NativeBitCount - blockSize);
        return j * blockSize + Select(~bitAlignedBlock, (int)position);
    }

    // =========================================================================
    // Serialization
    // =========================================================================

    public static SuccinctCompressedBits Read(BinaryReader reader)
    {
        var size = reader.ReadNUInt();
        var setBitsCount = reader.ReadNUInt();

        var classValues = Bits.Bits.Read(reader);
        var offsetValues = Bits.Bits.Read(reader);
        var rankSamples = QuasiSuccinctIndices.Read(reader);
        var offsetPosSamples = QuasiSuccinctIndices.Read(reader);

        return new SuccinctCompressedBits(
            size: size,
            setBitsCount: setBitsCount,
            rankSamples: rankSamples,
            offsetPositionSamples: offsetPosSamples,
            classValues: classValues,
            offsetValues: offsetValues);
    }

    public static SuccinctCompressedBits Read(string filename)
    {
        using var reader = new BinaryReader(
            new FileStream(filename, FileMode.Open, FileAccess.Read));
        return Read(reader);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteNUInt(_size);
        writer.WriteNUInt(_setBitsCount);

        _classValues.Write(writer);
        _offsetValues.Write(writer);
        _rankSamples.Write(writer);
        _offsetPositionSamples.Write(writer);
    }

    public void Write(string filename)
    {
        using var writer = new BinaryWriter(
            new FileStream(filename, FileMode.Create, FileAccess.Write));
        Write(writer);
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        var bv = (SuccinctCompressedBits)obj;
        return _size == bv._size &&
            _setBitsCount == bv._setBitsCount &&
            _classValues.Equals(bv._classValues) &&
            _offsetValues.Equals(bv._offsetValues);
    }

    public override int GetHashCode()
        => _classValues.GetHashCode() * _offsetValues.GetHashCode();

    public static bool operator ==(SuccinctCompressedBits left, SuccinctCompressedBits right)
        => left.Equals(right);

    public static bool operator !=(SuccinctCompressedBits left, SuccinctCompressedBits right)
        => !(left == right);
}