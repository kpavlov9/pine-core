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

using PineEffects.src.Monads.MaybeMonad;

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

    public IEnumerable<nuint> Data => _data;

    private Maybe<SuccinctBitsBuilder> _succintBitsBuilder;
    private Maybe<SuccinctCompressedBitsBuilder> _succintCompressedBitsBuilder;

    private static nuint GetCapacity(nuint initialCapacity) =>
        (initialCapacity + NativeBitCountMinusOne) / NativeBitCount;

    public BitsBuilder(nuint initialCapacity)
    {
        _data = new List<nuint>(new nuint[GetCapacity(initialCapacity)]);
    }

    public BitsBuilder()
    {
        _data = new List<nuint>();
    }

    public BitsBuilder(IReadOnlyList<nuint> data)
    {
        _data = new();

        InitializeFromData(
            inputData: data,
            inputDataSize: (nuint)(NativeBitCount * data.Count),
            outputData: _data,
            outputPosition: out _position);
    }

    public BitsBuilder(IEnumerable<nuint> data)
    {
        _data = new();

        InitializeFromData(
            inputData: data,
            inputDataSize: (nuint)(NativeBitCount * data.Count()),
            outputData: _data,
            outputPosition: out _position);
    }

    public BitsBuilder(IBits bits)
    {
        _data = new();
        InitializeFromBits(bits);
    }

    public BitsBuilder(byte[] bytes)
        : this((nuint)bytes.Length * BitCountInByte)
    {
        InitialFill(bytes);
    }

    public BitsBuilder(IEnumerable<byte> bytes)
        : this(bytes.ToArray())
    {
    }

    private void InitialFill(ReadOnlySpan<byte> bits)
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
    }

    public void AddBytes(IReadOnlyList<byte> bytes)
    {
        for (var i = 0; i < bytes.Count; i++)
        {
            AddBits(bytes[i], BitCountInByte);
        }
    }

    public void Clear()
    {
        _data.Clear();
        _position = 0;
    }

    public void ClearAndInitialize(IBits bits)
    {
        Clear();
        InitializeFromBits(bits);
    }

    private static void InitializeFromData(
        IEnumerable<nuint> inputData,
        nuint inputDataSize,
        in List<nuint> outputData,
        out nuint outputPosition)
    {
        if(outputData.Count > 0)
        {
            throw new Exception(
                $"The data size should be 0 before initialization, but it is {outputData.Count}.");
        }

        outputData.AddRange(inputData);
        outputPosition = inputDataSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitializeFromBits(IBits bits)
        => InitializeFromData(
            inputData: bits.Data,
            inputDataSize: bits.Size,
            outputData: _data,
            outputPosition: out _position);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBit(nuint position) =>
        Bits.GetBit(position: position, data: _data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bits Build()
        => new(_position, _data.ToImmutableArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IBitIndices IBitsBuilder.BuildBitIndices() => Build();

    IBitIndices IBitsBuilder.ClearAndBuildBitIndices(IBits bits)
    {
        Clear();
        _data.AddRange(bits.Data);
        _position = bits.Size;
        return Build();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISuccinctIndices BuildSuccinctIndices()
        => _succintBitsBuilder
            .Reduce(
                @default: new SuccinctBitsBuilder(this),
                maybeAfter: out _succintBitsBuilder)
            .Build();

    public ISuccinctIndices ClearAndBuildSuccinctIndices(IBits bits)
    {
        Clear();
        InitializeFromBits(bits);

        return BuildSuccinctIndices();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISuccinctCompressedIndices BuildSuccinctCompressedIndices()
        => _succintCompressedBitsBuilder
            .Reduce(
                @default: new SuccinctCompressedBitsBuilder(this),
                maybeAfter: out _succintCompressedBitsBuilder)
            .Build();

    public ISuccinctCompressedIndices ClearAndBuildSuccinctCompressedIndices(IBits bits)
    {
        Clear();
        InitializeFromBits(bits);

        return BuildSuccinctCompressedIndices();
    }

    public void Set(nuint position)
    {
        int indexInList = (int)(position / NativeBitCount);

        while (indexInList >= _data.Count)
        {
            _data.Add(0);
        }

        int offset = (int)(position % NativeBitCount);
        nuint block = _data[indexInList];
        nuint mask = NUIntOne << NativeBitCountMinusOne - offset;
        block |= mask;
        _data[indexInList] = block;

        var positionPlusOne = position + 1;

        if (positionPlusOne > _position)
        {
            _position = positionPlusOne;
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
        while (indexInList >= _data.Count)
        {
            _data.Add(0);
        }

        int offset = (int)(position % NativeBitCount);
        nuint block = _data[indexInList];
        nuint mask = NUIntOne << NativeBitCountMinusOne - offset;
        block &= ~mask;
        _data[indexInList] = block;

        var positionPlusOne = position + 1;

        if (positionPlusOne > _position)
        {
            _position = positionPlusOne;
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
