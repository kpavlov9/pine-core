using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.SuccinctOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctBits;

/// <summary>
/// Builds the bits sequence <see cref="SuccinctBits"/>
/// by adding bit by bit or an array of bits.
/// </summary>
public sealed class SuccinctBitsBuilder: IBitsContainer, IBitsBuilder, IBits
{
    private readonly List<nuint> _values;
    private readonly List<nuint> _ranks;
    private nuint _size;
    private nuint _setBitsCount;

    public nuint Size => _size;

    public IEnumerable<nuint> Data => _values.Select(ReverseBits);

    public nuint SetBitsCount => _setBitsCount;
    public nuint UnsetBitsCount => _size - _setBitsCount;

    private SuccinctCompressedBitsBuilder? _succintCompressedBitsBuilder;

    public SuccinctBitsBuilder()
    {
        _values = new List<nuint>();
        _ranks = new List<nuint>();
        _size = 0;
        _setBitsCount = 0;
    }

    private SuccinctBitsBuilder(
        List<nuint> values,
        List<nuint> ranks,
        nuint size,
        nuint setBitsCount)
    {
        _values = values;
        _ranks = ranks;
        _size = size;
        _setBitsCount = setBitsCount;
    }

     public void PushZeroes(int bitsCount)
     {
        if(bitsCount > 0)
        {
            Unset((nuint)bitsCount - 1);
            return;
        }
        
        throw new ArgumentException(
            "The bits count should be great than zero.",
            nameof(bitsCount));
     }

    public void PushOnes(int bitsCount)
    {
        var i = 0;

        do
        {
            Set(_size);
        }
        while(i++ < bitsCount);
    }

    public void Add(bool bitValue)
    {
        if(bitValue)
        {
            Set(_size);
            return;
        }

        Unset(_size);
    }

    public SuccinctBitsBuilder(IBitsContainer bits)
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
        IBitsContainer bits)
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
        IBitsContainer bits,
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

    public SuccinctBits ClearAndInitialize(IBitsContainer bits)
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

        _values[qSmall] = newValue;
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
    public IBits BuildBits() => Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISuccinctBits BuildSuccinctBits() => Build();

    public ISuccinctCompressedBits BuildSuccinctCompressedBits()
    {
        _succintCompressedBitsBuilder ??= new SuccinctCompressedBitsBuilder(this);

        return _succintCompressedBitsBuilder.Build();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Bits.Bits.GetHashCode(values: _values);

    public IBits ClearAndBuildBits(IBitsContainer bits)
        => ClearAndInitialize(bits);

    public ISuccinctBits ClearAndBuildSuccinctBits(IBitsContainer bits)
        => ClearAndInitialize(bits);

    public ISuccinctCompressedBits ClearAndBuildSuccinctCompressedBits(IBitsContainer bits)
    {
        ClearAndInitialize(bits);
        return BuildSuccinctCompressedBits();
    }

    public IBitsBuilder Clone() => new SuccinctBitsBuilder(
        values: _values.ToList(),
        ranks: _ranks.ToList(),
        size: _size,
        setBitsCount: _setBitsCount);
}
