using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.SuccinctOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using System.Numerics;
using System.Runtime.InteropServices;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctBits;

/// <summary>
/// Builds the bits sequence <see cref="SuccinctBits"/>
/// by adding bit by bit or an array of bits.
/// </summary>
public sealed class SuccinctBitsBuilder : IBitsContainer, IBitsBuilder, IBits
{
    private readonly List<nuint> _values;
    private readonly List<nuint> _ranks;
    private nuint _size;
    private nuint _setBitsCount;

    public nuint Size => _size;
    public IEnumerable<nuint> Data => _values.Select(ReverseBits);
    public nuint SetBitsCount => _setBitsCount;
    public nuint UnsetBitsCount => _size - _setBitsCount;

    public SuccinctBitsBuilder()
    {
        _values = [];
        _ranks = [];
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

    public SuccinctBitsBuilder(IBitsContainer bits)
    {
        _values = [];
        _ranks = [];
        InitializeFromBits(bits);
    }

    public SuccinctBitsBuilder(nuint initialSize)
    {
        var capacity = (int)((initialSize + NativeBitCountMinusOne) / NativeBitCount);
        _values = new List<nuint>(capacity);
        for (int i = 0; i < capacity; i++)
            _values.Add(0);
        
        _ranks = [];
        _size = initialSize;
        _setBitsCount = 0;
    }

    public SuccinctBitsBuilder(IEnumerable<byte> bytes)
    {
        _values = [];
        _ranks = [];
        InitializeFromBytes(bytes);
    }

    private void InitializeFromBits(IBitsContainer bits)
    {
        _values.Clear();
        _ranks.Clear();

        foreach (nuint bitValues in bits.Data)
        {
            _values.Add(ReverseBits(bitValues));
        }

        _size = bits.Size;
        _setBitsCount = 0;
    }

    private void InitializeFromBytes(IEnumerable<byte> bytes)
    {
        _values.Clear();
        _ranks.Clear();

        nuint value = 0;
        nuint i = 0;
        
        foreach (byte b in bytes)
        {
            value = (value << BitCountInByte) | b;
            i++;
            
            if (i % BytesCountInValue == 0)
            {
                _values.Add(ReverseBits(value));
                value = 0;
            }
        }
        
        if (i % BytesCountInValue > 0)
        {
            _values.Add(ReverseBits(value));
        }
        
        _size = i * BitCountInByte;
        _setBitsCount = 0;
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
        InitializeFromBits(bits);
        return Build();
    }

    public void PushZeroes(int bitsCount)
    {
        if (bitsCount > 0)
        {
            Unset((nuint)bitsCount - 1);
        }
    }

    public void PushOnes(int bitsCount)
    {
        for (int i = 0; i < bitsCount; i++)
        {
            Set(_size);
        }
    }

    public void Add(bool bitValue)
    {
        if (bitValue)
            Set(_size);
        else
            Unset(_size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBit(nuint position)
    {
        if (position >= _size)
        {
            throw new IndexOutOfRangeException(
                $"Position {position} exceeds sequence length {_size}");
        }

        int qSmall = (int)position / SmallBlockSize;
        int rSmall = (int)position % SmallBlockSize;

        return ((_values[qSmall] >> rSmall) & 1) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(nuint position)
    {
        if (position >= _size)
        {
            _size = position + 1;
        }

        int qSmall = (int)position / SmallBlockSize;
        int rSmall = (int)position % SmallBlockSize;

        while (qSmall >= _values.Count)
        {
            _values.Add(0);
        }

        nuint mask = NUIntOne << rSmall;
        nuint newValue = _values[qSmall] | mask;

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

        int qSmall = (int)position / SmallBlockSize;
        int rSmall = (int)position % SmallBlockSize;

        while (qSmall >= _values.Count)
        {
            _values.Add(0);
        }

        nuint mask = NUIntOne << rSmall;
        nuint newValue = _values[qSmall] & ~mask;

        if (_values[qSmall] != newValue)
        {
            _setBitsCount--;
        }

        _values[qSmall] = newValue;
    }

    /// <summary>
    /// Build using Span-based PopCount for efficiency.
    /// </summary>
    public SuccinctBits Build()
    {
        _ranks.Clear();
        _ranks.Capacity = (_values.Count + BlockRate - 1) / BlockRate;
        _setBitsCount = 0;

        // Use Span for efficient iteration
        var span = CollectionsMarshal.AsSpan(_values);
        
        for (int i = 0; i < span.Length; i++)
        {
            if (i % BlockRate == 0)
            {
                _ranks.Add(_setBitsCount);
            }

            _setBitsCount += (nuint)BitOperations.PopCount(span[i]);
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
        var builder = new SuccinctCompressedBitsBuilder(this);
        return builder.Build();
    }

    public IBits ClearAndBuildBits(IBitsContainer bits) => ClearAndInitialize(bits);
    public ISuccinctBits ClearAndBuildSuccinctBits(IBitsContainer bits) => ClearAndInitialize(bits);

    public ISuccinctCompressedBits ClearAndBuildSuccinctCompressedBits(IBitsContainer bits)
    {
        ClearAndInitialize(bits);
        return BuildSuccinctCompressedBits();
    }

    public IBitsBuilder Clone() => new SuccinctBitsBuilder(
        _values.ToList(),
        _ranks.ToList(),
        _size,
        _setBitsCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Bits.Bits.GetHashCode(values: _values);
}