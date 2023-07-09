using System.Numerics;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using KGIntelligence.PineCore.Helpers.Utilities;
using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.SuccinctOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitsBuilderHelper;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;

/// <summary>
/// Builds the bits sequence <see cref="Bits"/>
/// by adding bit by bit or an array of bits.
/// </summary>
public sealed class BitsBuilder : IBits, IBitIndices, IBitsBuilder
{
    private readonly List<nuint> _data;

    private nuint _position = 0;

    public nuint Size => _position;

    public IReadOnlyList<nuint> Data => _data;

    private static int GetCapacity(ulong initialCapacity) =>
        ((int)initialCapacity + NativeBitCountMinusOne) / NativeBitCount;

    public BitsBuilder(ulong initialCapacity)
    {
        _data = new List<nuint>(new nuint[GetCapacity(initialCapacity)]);
    }

    public BitsBuilder(IEnumerable<nuint> bits)
    {
        _data = bits.ToList();
    }

    public BitsBuilder(byte[] bytes)
        : this((nuint)bytes.Length * BitCountInByte)
    {
        Fill(bytes);
    }

    public BitsBuilder(IEnumerable<byte> bytes)
        : this(bytes.ToArray())
    {
    }

    private BitsBuilder Fill(ReadOnlySpan<byte> bits)
    {
        var marrowBits = MemoryMarshal.Cast<byte, Vector<byte>>(bits);

        //[Hack]: unsafe operation and we shouldn't add any items to the list while operating with the span.
        var marrowData =
            MemoryMarshal.Cast<nuint, Vector<byte>>(CollectionsMarshal.AsSpan(_data));

        for (var i = 0; i < marrowBits.Length; i++)
        {
            marrowData[i] = marrowBits[i];
        }

        var cutoff = Vector<byte>.Count * marrowBits.Length;

        _position = (uint)(cutoff * BitCountInByte);

        for (var i = cutoff; i < bits.Length; i++)
        {
            Add(bits[i], BitCountInByte);
        }

        return this;
    }

    public static BitsBuilder OfFixedLength(nuint length)
        => new(new nuint[GetCapacity(length)]);

    public static BitsBuilder OfFixedLength(byte[] bits)
        => OfFixedLength((nuint)(bits.Length * BitCountInByte))
           .Fill(bits);

    public void Clear()
    {
        _data.Clear();
        _position = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBit(nuint position) =>
        Bits.GetBit(position: position, data: _data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bits Build()
        => new(_position, _data.ToImmutableArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IBitIndices IBitsBuilder.BuildBitIndices() => Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ISuccinctIndices IBitsBuilder.BuildSuccinctIndices()
        => new SuccinctBitsBuilder(_data).Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ISuccinctCompressedIndices IBitsBuilder.BuildSuccinctCompressedIndices()
        => new SuccinctCompressedBitsBuilder(_data).Build();

    public void Set(nuint position)
    {
        int indexInList = (int)(position / NativeBitCount);
        int offset = (int)(position % NativeBitCount);
        nuint block = _data[indexInList];
        nuint mask = NUIntOne << NativeBitCountMinusOne - offset;
        block |= mask;
        _data[indexInList] = block;

        if (position + 1 >= _position)
        {
            _position = position + 1;
        }
    }

    public nuint FetchBits(nuint position, int bitsCount)
    {
        ValidateFetchBits(bitsCount);

        return Bits.InternalFetch(
            position: position,
            bitsCount: bitsCount,
            data: _data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint FetchBits(nuint position)
    => Bits.InternalFetch(
        position: position,
        bitsCount: NativeBitCount,
        data: _data);

    public void Unset(nuint position)
    {
        int indexInList = (int)(position / NativeBitCount);
        int offset = (int)(position % NativeBitCount);
        nuint block = _data[indexInList];
        nuint mask = NUIntOne << NativeBitCountMinusOne - offset;
        block &= ~mask;
        _data[indexInList] = block;

        if (position + 1 >= _position)
        {
            _position = position + 1;
        }
    }

    public void AddBits(nuint bits, int bitsCount)
    {
        if (bitsCount == 0)
        {
            return;
        }

        if (bitsCount < 0 || bitsCount > NativeBitCount)
        {
            throw new ArgumentOutOfRangeException(
                @$"The given bits count of '{bitsCount}'
 exceeds the valid range: [0, {sizeof(long)}]."
            );
        }

        var left = _position;
        var right = left + (nuint)bitsCount - 1;

        var leftIndexInList = (int)(left / NativeBitCount);
        var offsetInList = (int)(left % NativeBitCount);

        var rightIndexInList = (int)(right / NativeBitCount);

        if (leftIndexInList != rightIndexInList)
        {
            // LongSizebit boundary is crossed over
            var bitsCountForLeft = NativeBitCount - offsetInList;
            var bitsCountForRight = bitsCount - bitsCountForLeft;
            var bitsForLeft = bits >> bitsCountForRight;

            AddBits(bitsForLeft, bitsCountForLeft);
            AddBits(bits, bitsCountForRight);
        }
        else
        {
            // no boundary crossing
            if (leftIndexInList >= _data.Count)
            {
                _data.Add(NUIntZero);
            }
            var block = _data[leftIndexInList];

            var mask = bitsCount == NativeBitCount
                ? nuint.MaxValue
                : (NUIntOne << bitsCount) - NUIntOne;

            bits &= mask;
            bits <<= NativeBitCount - (offsetInList + bitsCount);
            block |= bits;
            _data[leftIndexInList] = block;
            _position += (uint)bitsCount;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(ulong bits, int bitsCount)
        => this.PushNative(bits, bitsCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(uint bits, int bitsCount)
        => AddBits((nuint)bits, bitsCount);

    public void AddBits(IList<byte> values)
    {
        foreach (byte value in values)
        {
            Add(value, sizeof(byte) * 8);
        }
    }

    public void AddSetBits(int bitsCount)
    {
        for (var i = 0; i < bitsCount / NativeBitCount; i++)
        {
            AddBits(nuint.MaxValue, NativeBitCount);
        }

        var r = bitsCount % NativeBitCount;

        if (r > 0)
        {
            AddBits(nuint.MaxValue, r);
        }
    }

    public void AddUnsetBits(int bitsCount)
    {
        for (var i = 0; i < bitsCount / NativeBitCount; i++)
        {
            AddBits(NUIntZero, NativeBitCount);
        }

        var r = bitsCount % NativeBitCount;

        if (r > 0)
        {
            AddBits(NUIntZero, r);
        }
    }

    public void Add(bool bitValue)
    {
        if (bitValue)
        {
            AddSetBits(1);
            return;
        }
        AddUnsetBits(1);
    }
}
