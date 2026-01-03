using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.Unions;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.SuccinctOps;
using static KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.Bits.Bits;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctBits;

/// <summary>
/// Not compressed bit sequence offering rank and select queries.
/// </summary>
public readonly struct SuccinctBits : IBitsContainer, ISerializableBits<SuccinctBits>, ISuccinctBits
{
    private readonly ImmutableArray<nuint> _values;
    private readonly ImmutableArray<nuint> _ranks;
    private readonly nuint _size;
    private readonly nuint _setBitsCount;

    public nuint Size => _size;
    public IEnumerable<nuint> Data => _values.Select(ReverseBits);
    public nuint SetBitsCount => _setBitsCount;
    public nuint UnsetBitsCount => _size - _setBitsCount;

    public SuccinctBits(
        nuint size,
        nuint setBitsCount,
        ImmutableArray<nuint> values,
        ImmutableArray<nuint> ranks)
    {
        _size = size;
        _setBitsCount = setBitsCount;
        _values = values;
        _ranks = ranks;
    }

    /// <summary>
    /// Get bit at position using branch-free extraction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBit(nuint position)
    {
        if (position >= _size)
        {
            throw new IndexOutOfRangeException(
                $"Position {position} exceeds sequence length {_size}");
        }

        int qSmall = ((int)position / SmallBlockSize);
        int rSmall = ((int)position % SmallBlockSize);
        
        // Branch-free: extract single bit
        return ((_values[qSmall] >> rSmall) & 1) != 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint RankSetBits(nuint bitPositionCutoff)
    {
        if (bitPositionCutoff > _size)
            bitPositionCutoff = _size;
        
        if (bitPositionCutoff == 0)
            return 0;

        // Use concrete ImmutableArray directly instead of IReadOnlyList
        return RankSetBitsOptimized(bitPositionCutoff, _ranks, _values);
    }

    /// <summary>
    /// Optimized rank using direct array access (no interface dispatch).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint RankSetBitsOptimized(
        nuint bitPositionCutoff,
        ImmutableArray<nuint> ranks,
        ImmutableArray<nuint> values)
    {
        bitPositionCutoff--;

        // Calculate large block position
        int qLarge = (int)bitPositionCutoff / LargeBlockSize;
        nuint rank = ranks[qLarge];

        // Calculate small block positions
        int qSmall = (int)bitPositionCutoff / SmallBlockSize;
        int rSmall = (int)bitPositionCutoff % SmallBlockSize;

        int begin = qLarge * BlockRate;

        // Sum PopCounts for intermediate blocks
        for (int j = begin; j < qSmall; j++)
        {
            rank += (nuint)BitOperations.PopCount(values[j]);
        }

        // Handle final partial block
        nuint lastValue = values[qSmall];
        lastValue <<= SmallBlockSize - rSmall - 1;
        rank += (nuint)BitOperations.PopCount(lastValue);

        return rank;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint RankUnsetBits(nuint bitPositionCutoff)
    {
        if (bitPositionCutoff > _size)
            bitPositionCutoff = _size;
        
        if (bitPositionCutoff == 0)
            return 0;

        return RankUnsetBitsOptimized(bitPositionCutoff, _ranks, _values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint RankUnsetBitsOptimized(
        nuint bitPositionCutoff,
        ImmutableArray<nuint> ranks,
        ImmutableArray<nuint> values)
    {
        bitPositionCutoff--;

        int qLarge = (int)bitPositionCutoff / LargeBlockSize;
        nuint rank = (nuint)(qLarge * LargeBlockSize) - ranks[qLarge];

        int qSmall = (int)bitPositionCutoff / SmallBlockSize;
        int rSmall = (int)bitPositionCutoff % SmallBlockSize;

        int begin = qLarge * BlockRate;

        for (int j = begin; j < qSmall; j++)
        {
            rank += (nuint)BitOperations.PopCount(~values[j]);
        }

        nuint lastValue = ~values[qSmall];
        lastValue <<= (SmallBlockSize - rSmall - 1);
        rank += (nuint)BitOperations.PopCount(lastValue);

        return rank;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint SelectSetBits(nuint bitCountCutoff)
    {
        if (bitCountCutoff >= SetBitsCount)
            return _size;

        return SelectSetBits(bitCountCutoff, _ranks, _values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint SelectSetBits(
        nuint bitCountCutoff,
        ImmutableArray<nuint> ranks,
        ImmutableArray<nuint> values)
    {
        // Binary search over rank samples
        int left = 0;
        int right = ranks.Length;
        
        while (left < right)
        {
            int pivot = (left + right) >> 1;
            
            if (bitCountCutoff < ranks[pivot])
                right = pivot;
            else
                left = pivot + 1;
        }
        right--;

        bitCountCutoff -= ranks[right];
        int j = right * BlockRate;
        
        // Linear scan through blocks
        while (true)
        {
            uint rank = (uint)BitOperations.PopCount(values[j]);

            if (bitCountCutoff < rank)
                break;
            
            j++;
            bitCountCutoff -= rank;
        }

        return (nuint)(j * SmallBlockSize + SelectOfReversed(values[j], (int)bitCountCutoff));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint SelectUnsetBits(nuint bitCountCutoff)
    {
        if (bitCountCutoff >= UnsetBitsCount)
            return _size;

        return SelectUnsetBitsOptimized(bitCountCutoff, _ranks, _values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint SelectUnsetBitsOptimized(
        nuint bitCountCutoff,
        ImmutableArray<nuint> ranks,
        ImmutableArray<nuint> values)
    {
        int left = 0;
        int right = ranks.Length;
        
        while (left < right)
        {
            int pivot = (left + right) >> 1;
            nuint rank = (nuint)(pivot * LargeBlockSize) - ranks[pivot];
            
            if (bitCountCutoff < rank)
                right = pivot;
            else
                left = pivot + 1;
        }
        right--;

        bitCountCutoff -= (nuint)(right * LargeBlockSize) - ranks[right];
        int j = right * BlockRate;
        
        while (true)
        {
            uint rank = (uint)BitOperations.PopCount(~values[j]);

            if (bitCountCutoff < rank)
                break;
            
            j++;
            bitCountCutoff -= rank;
        }

        return (nuint)(j * SmallBlockSize + SelectOfReversed(~values[j], (int)bitCountCutoff));
    }

    // =========================================================================
    // Serialization
    // =========================================================================

    public static SuccinctBits Read(BinaryReader reader)
    {
        var size = reader.ReadUInt32();
        var setBitsCount = reader.ReadUInt32();

        var vSize = reader.ReadInt32();
        Span<nuint> values = stackalloc nuint[vSize];
        ReadNUInt(values, reader);

        var rSize = reader.ReadInt32();
        Span<nuint> ranks = stackalloc nuint[rSize];
        ReadNUInt(ranks, reader);

        return new SuccinctBits(
            size,
            setBitsCount,
            values.ToImmutableArray(),
            ranks.ToImmutableArray());
    }

    public static SuccinctBits Read(string filename)
    {
        using var reader = new BinaryReader(
            new FileStream(filename, FileMode.Open, FileAccess.Read));
        return Read(reader);
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(GetUIntLittleEndian(_size));
        writer.Write(GetUIntLittleEndian(_setBitsCount));

        var vSize = _values.Length;
        writer.Write(vSize);
        WriteNUInt(_values, vSize, writer);

        var rSize = _ranks.Length;
        writer.Write(rSize);
        WriteNUInt(_ranks, rSize, writer);
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

        var bits = (SuccinctBits)obj;
        return _size == bits._size && _values.SequenceEqual(bits._values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Bits.Bits.GetHashCode(values: _values);

    public static bool operator ==(SuccinctBits left, SuccinctBits right)
        => left.Equals(right);

    public static bool operator !=(SuccinctBits left, SuccinctBits right)
        => !(left == right);
}