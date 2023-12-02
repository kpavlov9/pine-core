using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.SuccinctOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;

using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;
using PineEffects.src.Monads.MaybeMonad;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices;

/// <summary>
/// Builds the bits sequence <see cref="SuccinctBits"/>
/// by adding bit by bit or an array of bits.
/// </summary>
public sealed class SuccinctBitsBuilder: IBits, IBitsBuilder, IBitIndices
{
    private readonly List<nuint> _values;
    private readonly List<nuint> _ranks;
    private nuint _size;
    private nuint _setBitsCount;

    public nuint Size => _size;

    public IEnumerable<nuint> Data => _values.Select(ReverseBits);

    public nuint SetBitsCount => _setBitsCount;
    public nuint UnsetBitsCount => _size - _setBitsCount;

    private Maybe<SuccinctCompressedBitsBuilder> _succintCompressedBitsBuilder;

    public SuccinctBitsBuilder()
    {
        _values = new List<nuint>();
        _ranks = new List<nuint>();
        _size = 0;
        _setBitsCount = 0;
    }

    public SuccinctBitsBuilder(IBits bits)
    {
        _values = new List<nuint>();
        _ranks = new List<nuint>();

        InitializeFromBits(bits: bits);
    }

    public SuccinctBitsBuilder(nuint initialSize)
    {
        _values =
            new List<nuint>(
                new nuint[
                    (initialSize + NativeBitCountMinusOne) / NativeBitCount
                    ]);
        _ranks = new List<nuint>();
        _size = initialSize;
        _setBitsCount = 0;
    }

    public SuccinctBitsBuilder(IEnumerable<byte> bytes)
    {
        _values = new List<nuint>();
        _ranks = new List<nuint>();

        InitializeFromBytes(bytes);
    }

    private SuccinctBits InitializeFromBits(
        IBits bits)
    {
        _values.Clear();
        _ranks.Clear();

        ConstructFromBits(
            bits: bits,
            values: _values,
            size: out _size);

        return Build();
    }

    private SuccinctBits InitializeFromBytes(
        IEnumerable<byte> bytes)
    {
        _values.Clear();
        _ranks.Clear();

        ConstructFromBytes(
            bytes: bytes,
            values: _values,
            size: out _size);

        return Build();
    }

    private static void ConstructFromBytes(
      IEnumerable<byte> bytes,
      List<nuint> values,
      out nuint size)
    {
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

    private static void ConstructFromBits(
        IBits bits,
        List<nuint> values,
        out nuint size)
    {
        foreach (nuint bitValues in bits.Data)
        {
            values.Add(ReverseBits(bitValues));
        }

        size = bits.Size;
    }

    public void Clear()
    {
        _values.Clear();
        _ranks.Clear();
        _size = 0;
        _setBitsCount = 0;
    }

    public SuccinctBits ClearAndInitialize(IBits bits)
    {
        Clear();
        return InitializeFromBits(bits: bits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBit(nuint position)
    {
        if (position >= _size)
        {
            throw new IndexOutOfRangeException(
                $"The argument {nameof(position)} exceeds the sequence length {_size}");
        }

        GetBlockPositions(
            position,
            out var qSmall,
            out var rSmall);

        GetMask(rSmall, out var mask);
        return (_values[qSmall] & mask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(nuint position)
    {
        if (position >= _size)
        {
            _size = position + 1;
        }

        GetBlockPositions(
            position,
            out var qSmall,
            out var rSmall);

        while (qSmall >= _values.Count)
        {
            _values.Add(0);
        }

        GetMask(rSmall, out var mask);

        var newValue = _values[qSmall] | mask;

        if (_values[qSmall] != newValue)
        {
            _setBitsCount++;
        }

        _values[qSmall] = _values[qSmall] | mask;
    }

    public void Unset(nuint position)
    {
        if (position >= _size)
        {
            _size = position + 1;
        }

        GetBlockPositions(
            position,
            out var qSmall,
            out var rSmall);

        while (qSmall >= _values.Count)
        {
            _values.Add(0);
        }

        GetMask(rSmall, out var mask);
        var newValue = _values[qSmall] & ~mask;

        if (_values[qSmall] != newValue)
        {
            _setBitsCount--;
        }

        _values[qSmall] = newValue;
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

            _setBitsCount += PopCount(value: _values[i]);
        }

        return new SuccinctBits(
            _size,
            _setBitsCount,
            _values.ToImmutableArray(),
            _ranks.ToImmutableArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBitIndices BuildBitIndices() => Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISuccinctIndices BuildSuccinctBits() => Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISuccinctCompressedIndices BuildSuccinctCompressedIndices()
        => _succintCompressedBitsBuilder
            .Reduce(
                @default: new SuccinctCompressedBitsBuilder(this),
                monadAfter: out _succintCompressedBitsBuilder)
            .Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Bits.GetHashCode(values: _values);

    public IBitIndices ClearAndBuildBitIndices(IBits bits)
        => ClearAndInitialize(bits);

    public ISuccinctIndices ClearAndBuildSuccinctIndices(IBits bits)
        => ClearAndInitialize(bits);

    public ISuccinctCompressedIndices ClearAndBuildSuccinctCompressedIndices(IBits bits)
    {
        ClearAndInitialize(bits);
        return BuildSuccinctCompressedIndices();
    }
}
