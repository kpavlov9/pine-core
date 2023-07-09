using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.SuccinctOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;

using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices;

/// <summary>
/// Builds the bits sequence <see cref="SuccinctBits"/>
/// by adding bit by bit or an array of bits.
/// </summary>
public sealed class SuccinctBitsBuilder: IBitIndices, IBitsBuilder, ISuccinctIndices
{
    private readonly List<nuint> _values;
    private readonly List<nuint> _ranks;
    private nuint _size;
    private nuint _setBitsCount;

    public nuint Size => _size;

    public IReadOnlyList<nuint> Data => _values;

    public nuint SetBitsCount => _setBitsCount;
    public nuint UnsetBitsCount => _size - _setBitsCount;


    public SuccinctBitsBuilder()
    {
        _values = new List<nuint>();
        _ranks = new List<nuint>();
        _size = 0;
        _setBitsCount = 0;
    }

    public SuccinctBitsBuilder(IBits bits)
    {
        ConstructFromBits(
                bits: bits,
                values: out _values,
                ranks: out _ranks,
                size: out _size);
        Build();
    }

    public SuccinctBitsBuilder(IEnumerable<nuint> bits)
    {
        ConstructFromBits(
                bits: new BitsBuilder(bits),
                values: out _values,
                ranks: out _ranks,
                size: out _size);

        Build();
    }

    private static void ConstructFromBits(
        IBits bits,
        out List<nuint> values,
        out List<nuint> ranks,
        out nuint size)
    {
        values = new List<nuint>();
        ranks = new List<nuint>();

        foreach (nuint bitValues in bits.Data)
        {
            values.Add(ReverseBits(bitValues));
        }

        size = bits.Size;
    }

    public SuccinctBitsBuilder(int size)
    {
        _values =
            new List<nuint>(
                new nuint[
                    (size + NativeBitCountMinusOne) / NativeBitCount
                    ]);
        _ranks = new List<nuint>();
        _size = (nuint)size;
        _setBitsCount = 0;
    }

    public SuccinctBitsBuilder(IReadOnlyList<byte> bytes)
    {
        ConstructFromBytes(
            bytes: bytes,
            values: out _values,
            ranks: out _ranks,
            size: out _size);
    }

    public SuccinctBitsBuilder(IEnumerable<byte> bytes)
    {
        ConstructFromBytes(
            bytes: bytes,
            values: out _values,
            ranks: out _ranks,
            size: out _size);
    }

    private static void ConstructFromBytes(
        IEnumerable<byte> bytes,
        out List<nuint> values,
        out List<nuint> ranks,
        out nuint size)
    {
        values = new List<nuint>();
        ranks = new List<nuint>();

        nuint value = 0;
        nuint i = 0;
        foreach (byte b in bytes)
        {
            value = value << BitCountInByte | b;
            ++i;
            if (i % BytesCountInValue == 0)
            {
                values.Add(ReverseBits(value));
            }
        }
        if (i % BytesCountInValue > 0)
        {
            values.Add(ReverseBits(value));
        }
        size = i * BitCountInByte;
    }

    public void Clear()
    {
        _values.Clear();
        _ranks.Clear();
        _size = 0;
        _setBitsCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBit(nuint position)
    {
        if (position >= _size)
        {
            throw new IndexOutOfRangeException(
                $@"The argument {nameof(position)}
 exceeds the sequence length {_size}");
        }

        GetBlockPositions(
            position,
            out int qSmall,
            out int rSmall);

        GetMask(rSmall, out var mask);
        return (_values[qSmall] & mask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(nuint position)
    {
        if(position >= _size)
        {
            _size = position + 1;
        }

        GetBlockPositions(
            position,
            out int qSmall,
            out int rSmall);

        while (qSmall >= _values.Count)
        {
            _values.Add(0);
        }

        GetMask(rSmall, out var mask);

        _values[qSmall] = _values[qSmall] | mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint SelectSetBits(nuint bitCountCutoff)
        => SuccinctBits.SelectSetBits(
                bitCountCutoff: bitCountCutoff,
                setBitsCount: _setBitsCount,
                ranks: _ranks,
                values: _values);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint SelectUnsetBits(nuint bitCountCutoff)
    => SuccinctBits.SelectUnsetBits(
            bitCountCutoff: bitCountCutoff,
            unsetBitsCount: _size - _setBitsCount,
            ranks: _ranks,
            values: _values);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint RankUnsetBits(nuint bitPositionCutoff)
    {
        ValidatePosition(position: bitPositionCutoff, size: _size);
        return SuccinctBits.RankUnsetBits(
            bitPositionCutoff: bitPositionCutoff,
            ranks: _ranks,
            values: _values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint RankSetBits(nuint bitPositionCutoff)
    {
        ValidatePosition(position: bitPositionCutoff, size: _size);
        return SuccinctBits.RankSetBits(
            bitPositionCutoff: bitPositionCutoff,
            ranks: _ranks,
            values: _values);
    }

    public void Unset(nuint position)
    {
        if (position >= _size)
        {
            _size = position + 1;
        }

        GetBlockPositions(
            position,
            out int qSmall,
            out int rSmall);

        while (qSmall >= _values.Count)
        {
            _values.Add(0);
        }

        GetMask(rSmall, out var mask);

        _values[qSmall] = _values[qSmall] & ~mask;
    }

    public SuccinctBits Build()
    {
        _ranks.Clear();
        _ranks.Capacity = (_values.Count + BlockRate - 1) / BlockRate;
        _setBitsCount = 0;
        for (var i = 0; i < _values.Count; i++)
        {
            if (i % BlockRate == 0)
            {
                _ranks.Add(_setBitsCount);
            }
            _setBitsCount += unchecked((nuint)RankOfReversed(
                value: _values[i],
                bitPositionCutoff: SmallBlockSize,
                blockSize: SmallBlockSize));
        }

        return new SuccinctBits(
            _size,
            _setBitsCount,
            _values.ToImmutableArray(),
            _ranks.ToImmutableArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IBitIndices IBitsBuilder.BuildBitIndices() => Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ISuccinctIndices IBitsBuilder.BuildSuccinctIndices() => Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ISuccinctCompressedIndices IBitsBuilder.BuildSuccinctCompressedIndices() =>
        new SuccinctCompressedBitsBuilder(_values).Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Bits.GetHashCode(values: _values);
}
