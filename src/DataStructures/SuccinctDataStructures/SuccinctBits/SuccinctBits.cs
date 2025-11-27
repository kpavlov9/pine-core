using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.Unions;
using static KGIntelligence.PineCore.Helpers.Utilities.SuccinctOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
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

    public bool GetBit(nuint position)
    {
        ValidatePosition(
            position: position,
            size: _size);

        GetBlockPositions(
            position,
            out int qSmall,
            out int rSmall);

        GetMask(rSmall, out var mask);
        return (_values[qSmall] & mask) != 0;
    }

    public nuint SetBitsCount
        => _setBitsCount;

    public nuint UnsetBitsCount
        => _size - _setBitsCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint RankSetBits(nuint bitPositionCutoff)
    {
        ValidatePosition(position: bitPositionCutoff, size: _size);
        return RankSetBits(
            bitPositionCutoff: bitPositionCutoff,
            ranks: _ranks,
            values: _values);
    }

    internal static nuint RankSetBits(
        nuint bitPositionCutoff,
        IReadOnlyList<nuint> ranks,
        IReadOnlyList<nuint> values)
    {
        nuint rank = 0;

        if (bitPositionCutoff == 0)
        {
            return rank;
        }

        bitPositionCutoff--;

        CalculateInitialRank(
            ranks: ranks,
            bitPositionCutoff: bitPositionCutoff,
            rank: ref rank,
            qLarge: out var qLarge);

        CalculateRankSetBits(
            values: values,
            bitPositionCutoff: bitPositionCutoff,
            qLarge: qLarge,
            rank: ref rank);

        return rank;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint RankUnsetBits(nuint bitPositionCutoff)
    {
        ValidatePosition(position: bitPositionCutoff, size: _size);
        return RankUnsetBits(
        bitPositionCutoff: bitPositionCutoff,
        ranks: _ranks,
        values: _values);
    }

    internal static nuint RankUnsetBits(
        nuint bitPositionCutoff,
        IReadOnlyList<nuint> ranks,
        IReadOnlyList<nuint> values)
    {
        nuint rank = 0;

        if (bitPositionCutoff == 0)
        {
            return rank;
        }

        bitPositionCutoff--;

        CalculateInitialRank(
            ranks: ranks,
            bitPositionCutoff: bitPositionCutoff,
            rank: ref rank,
            qLarge: out var qLarge);

        CalculateRankUnsetBits(
            values: values,
            bitPositionCutoff: bitPositionCutoff,
            qLarge: qLarge,
            rank: ref rank);

        return rank;
    }

    private static void CalculateInitialRank(
        IReadOnlyList<nuint> ranks,
        nuint bitPositionCutoff,
        ref nuint rank,
        out int qLarge)
    {
        qLarge = (int)bitPositionCutoff / LargeBlockSize;
        rank = ranks[qLarge];
    }

    private static void CalculateRankSetBits(
        IReadOnlyList<nuint> values,
        nuint bitPositionCutoff,
        int qLarge,
        ref nuint rank)
    {
        GetBlockPositions(
             position: bitPositionCutoff,
             qSmall: out var qSmall,
             rSmall: out var rSmall);

        var begin = qLarge * BlockRate;

        for (var j = begin; j < qSmall; j++)
        {
            rank += PopCount(values[j]);
        }

        rank += unchecked((uint)RankOfReversed(
            value: values[qSmall],
            bitPositionCutoff: rSmall + 1,
            blockSize: SmallBlockSize));
    }

    private static void CalculateRankUnsetBits(
        IReadOnlyList<nuint> values,
        nuint bitPositionCutoff,
        int qLarge,
        ref nuint rank)
    {
        GetBlockPositions(
            position: bitPositionCutoff,
            qSmall: out var qSmall,
            rSmall: out var rSmall);

        var begin = qLarge * BlockRate;

        rank = (nuint)(qLarge * LargeBlockSize) - rank;

        for (var j = begin; j < qSmall; j++)
        {
            rank += PopCount(~values[j]);
        }

        rank += unchecked((uint)RankOfReversed(
            value: ~values[qSmall],
            bitPositionCutoff: rSmall + 1,
            blockSize: SmallBlockSize));
    }

    internal static nuint SelectSetBits(
        nuint bitCountCutoff,
        IReadOnlyList<nuint> ranks,
        IReadOnlyList<nuint> values)
    {
        int left = 0;
        int right = ranks.Count;
        while (left < right)
        {
            int pivot = left + right >> 1;
            var rank = ranks[pivot];

            if (bitCountCutoff < rank)
            {
                right = pivot;
            }
            else
            {
                left = pivot + 1;
            }
        }
        right--;

        bitCountCutoff -= ranks[right];
        int j = right * BlockRate;
        while (true)
        {
            uint rank = PopCount(values[j]);

            if (bitCountCutoff < rank)
            {
                break;
            }
            j++;
            bitCountCutoff -= rank;
        }

        return (nuint)(j * SmallBlockSize + SelectOfReversed(
            values[j],
            (int)bitCountCutoff));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint SelectSetBits(nuint bitCountCutoff)
        => bitCountCutoff >= SetBitsCount
            ? _size
            : SelectSetBits(
            bitCountCutoff: bitCountCutoff,
            ranks: _ranks,
            values: _values);

    internal static nuint SelectUnsetBits(
        nuint bitCountCutoff,
        IReadOnlyList<nuint> ranks,
        IReadOnlyList<nuint> values)
    {
        int left = 0;
        int right = ranks.Count;
        while (left < right)
        {
            int pivot = left + right >> 1;
            var rank = ranks[pivot];
            rank = (nuint)(pivot * LargeBlockSize) - rank;
            if (bitCountCutoff < rank)
            {
                right = pivot;
            }
            else
            {
                left = pivot + 1;
            }
        }
        right--;

        bitCountCutoff -= (nuint)(right * LargeBlockSize) - ranks[right];
        int j = right * BlockRate;
        while (true)
        {
            var rank = PopCount(~values[j]);

            if (bitCountCutoff < rank) { break; }
            j++;
            bitCountCutoff -= rank;
        }

        return (nuint)(j * SmallBlockSize + SelectOfReversed(
            ~values[j],
            (int)bitCountCutoff));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint SelectUnsetBits(nuint bitCountCutoff)
        => bitCountCutoff >= UnsetBitsCount
            ? _size
            : SelectUnsetBits(
                bitCountCutoff: bitCountCutoff,
                ranks: _ranks,
                values: _values);

    public static SuccinctBits Read(BinaryReader reader)
    {
        var size = reader.ReadUInt32();
        var setBitsCount = reader.ReadUInt32();

        var vSize = reader.ReadInt32();
        Span<nuint> values = stackalloc nuint[vSize];

        ReadNUInt(
            buffer: values,
            reader: reader);

        var rSize = reader.ReadInt32();
        Span<nuint> ranks = stackalloc nuint[rSize];

        ReadNUInt(
            buffer: ranks,
            reader: reader);

        return new SuccinctBits(
            size,
            setBitsCount,
            values.ToImmutableArray(),
            ranks.ToImmutableArray());
    }

    public static SuccinctBits Read(string filename)
    {
        using var reader = new BinaryReader(
            new FileStream(
                filename,
                FileMode.Open,
                FileAccess.Read));
        return Read(reader);
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(GetUIntLittleEndian(_size));
        writer.Write(GetUIntLittleEndian(_setBitsCount));

        var vSize = _values.Length;

        writer.Write(vSize);

        WriteNUInt(
            buffer: _values,
            cutoff: vSize,
            writer: writer);

        var rSize = _ranks.Length;

        writer.Write(rSize);

        WriteNUInt(
            buffer: _ranks,
            cutoff: rSize,
            writer: writer);
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
        {
            return false;
        }

        var bits = (SuccinctBits)obj;
        return _size == bits._size && Enumerable.SequenceEqual(
            _values,
            bits._values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Bits.Bits.GetHashCode(values: _values);

    public static bool operator ==(SuccinctBits left, SuccinctBits right)
        => left.Equals(right);

    public static bool operator !=(SuccinctBits left, SuccinctBits right)
        => !(left == right);
}
