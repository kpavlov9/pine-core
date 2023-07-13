using System.Diagnostics;
using System.Runtime.CompilerServices;

using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;

using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using static KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices.BitsBuilder;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices;

/// <summary>
/// Builds the bits sequence <see cref="SuccinctCompressedBits"/>
/// by adding bit by bit or an array of bits.
/// </summary>
public sealed class SuccinctCompressedBitsBuilder: IBitIndices, IBitsBuilder
{
    public nuint Size => _bits.Size;

    /// <summary>
    /// It is fixed to be
    /// <see cref="BlockSize"/> = <see cref="NativeBitCountMinusOne"/>.
    /// A higher value results in increased compression at the expense of
    /// computational overhead.
    /// <see cref="BlockSize"/> must not exceed
    /// <see cref="NativeBitCountMinusOne"/>,
    /// since the implementation of bit-blocks heavily relies on
    /// <see cref="nuint"/>.
    /// </summary>
    internal static readonly byte BlockSize = NativeBitCountMinusOne;

    internal const nuint SuperBlockFactor = 32;

    /// <summary>
    /// It is fixed to be
    /// <see cref="SuperBlockFactor"/> = <see cref="BlockSize"/> *
    /// <see cref="SuperBlockFactor"/>.
    /// </summary>
    internal static readonly nuint SuperBlockSize =
        BlockSize * SuperBlockFactor;

    /// <summary>
    /// Bits per class calculated with the formula
    /// log_2(<see cref="BlockSize"/> + 1).
    /// </summary>
    internal static readonly byte BitsPerClass =
        (byte)(Is32BitSystem ? 5 : 6);

    /// <summary>
    /// ClassCounts[0, m] = 0 and ClassCounts[n, 0] = 1 for all m, n.
    /// </summary>
    public static readonly nuint[][] ClassCounts =
        new nuint[BlockSize + 1][];

    /// <summary>
    /// It's defined by: log_2(ClassCounts[n, k]) k = 0, ..., n.
    /// </summary>
    internal static readonly uint[] ClassBitOffsets =
        new uint[BlockSize + 1];

    /// <summary>
    /// It will be calculated in the static initializer.
    /// </summary>
    private static readonly uint MaxBitsPerOffset = 0;

    private readonly BitsBuilder _bits;
    private nuint GetBlocksCount() =>
        (_bits.Size + BlockSize - 1) / BlockSize;
    private nuint GetSuperBlocksCount() =>
        (_bits.Size + SuperBlockSize - 1) / SuperBlockSize;

    static SuccinctCompressedBitsBuilder()
    {
        var classCounts = ClassCounts;
        for (var n = 0; n <= BlockSize; n++)
        {
            classCounts[n] = new nuint[n + 2];
            classCounts[n][0] = 1;
        }

        for (var n = 1; n <= BlockSize; n++)
        {
            for (var k = 1; k <= n; k++)
            {
                classCounts[n][k] =
                    classCounts[n - 1][k - 1] + classCounts[n - 1][k];
            }
            classCounts[n][n + 1] = 0;
        }

        for (var n = 0; n <= BlockSize; n++)
        {
            var elementsInTheClass = classCounts[BlockSize][n];
            var bits =
                (uint)Math.Ceiling(Math.Log(elementsInTheClass + 1, 2));

            ClassBitOffsets[n] = bits;
            MaxBitsPerOffset = Math.Max(MaxBitsPerOffset, bits);
        }
    }

    public SuccinctCompressedBitsBuilder()
    {
        _bits = new BitsBuilder(0);
    }

    public SuccinctCompressedBitsBuilder(IBits bits)
    {
        _bits = new BitsBuilder(bits);
    }

    public SuccinctCompressedBitsBuilder(nuint initialSize)
    {
        _bits = new BitsBuilder(initialSize);
    }

    public SuccinctCompressedBitsBuilder(IReadOnlyList<byte> bytes)
    {
        _bits = new BitsBuilder(bytes);
    }

    public SuccinctCompressedBitsBuilder(byte[] bytes)
    {
        _bits = new BitsBuilder(bytes);
    }

    public void Clear() => _bits.Clear();

    public void ClearAndInitialize(IBits bits)
        => _bits.ClearAndInitialize(bits);

    public void PushZeroes(int bitsCount)
        => _bits.AddUnsetBits(bitsCount);

    public void PushOnes(int bitsCount)
        => _bits.AddSetBits(bitsCount);

    public void Add(bool bitValue)
        => _bits.Add(bitValue);

    public void Set(nuint position)
        => _bits.Set(position);

    public void Unset(nuint position)
        => _bits.Unset(position);

    public bool GetBit(nuint position)
        => _bits.GetBit(position);

    public static nuint OffsetOf(nuint block, uint @class)
    {
        if (@class == 0 || @class == BlockSize)
        {
            return 0;
        }

        nuint offset = 0;
        for (var i = BlockSize - 1; i > 0; i--)
        {
            if ((block >> i & 1) == 1)
            {
                offset += ClassCounts[i][@class];
                --@class;
            }
        }
        return offset;
    }

    private void InitializeBuilders(
        int size,
        nuint blocksCount,
        out BitsBuilder classValuesBuilder,
        out BitsBuilder offsetValuesBuilder,
        out QuasiSuccinctBitsBuilder rankSamplesBuilder,
        out QuasiSuccinctBitsBuilder offsetPositionSamplesBuilder)
    {
        var superBlocksCount = (int)GetSuperBlocksCount();
        classValuesBuilder = OfFixedLength(BitsPerClass * blocksCount);
        offsetValuesBuilder = OfFixedLength(MaxBitsPerOffset * blocksCount);
        rankSamplesBuilder = new QuasiSuccinctBitsBuilder(
            superBlocksCount,
            size);
        offsetPositionSamplesBuilder = new QuasiSuccinctBitsBuilder(
            superBlocksCount,
            size);
    }

    private static void SplitIntoBlocks(
        nuint blocksCount,
        in Bits bits,
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
            {// AddBits a super-block summary for each SuperblockFactor block:
                offsetPositionSamplesBuilder.AddBits(offsetValuesBuilder.Size);
                rankSamplesBuilder.AddBits(rankSum);
            }

            // Encode each block as a pair:
            var position = i * BlockSize;
            var block = bits.FetchBits(position, BlockSize);
            var @class = PopCount(block);
            classValuesBuilder.Add(@class, BitsPerClass);

            var offset = OffsetOf(block, @class);
            offsetValuesBuilder.AddBits(offset, (int)ClassBitOffsets[@class]);

            rankSum += @class;
        }
    }

    private static void CheckEncoding(
        nuint blocksCount,
        in Bits bits,
        in Bits offsetValues,
        in Bits classValues,
        in QuasiSuccinctBits offsetPositionSamples)
    {
        for (nuint i = 0; i < blocksCount; i++)
        {
            var position = i * BlockSize;
            var block = bits.FetchBits(position, BlockSize);

            var @class = PopCount(block);

            var offset = OffsetOf(block, @class);

            var class2 =
                SuccinctCompressedBits.ClassOfBlock(i, classValues);

            var offset2 =
                SuccinctCompressedBits.OffsetOfBlock(
                    i,
                    @class,
                    offsetPositionSamples,
                    offsetValues,
                    classValues);

            var block2 = SuccinctCompressedBits.FetchBlock(
                    i,
                    @class,
                    offsetPositionSamples,
                    offsetValues,
                    classValues);

            Debug.Assert(
                @class == class2,
                $"class: {@class} != {class2} at block {i};");
            Debug.Assert(
                offset == offset2,
                $"offset: {offset} != {offset2} at block {i};");
            Debug.Assert(
                block == block2,
                @$"block: {Convert.ToString((long)block, 2)} !=
 {Convert.ToString((long)block2, 2)} at block {i};");

        }
    }
    private static SuccinctCompressedBits BuildSuccinctBits(
        int size,
        in Bits bits,
        nuint blocksCount,
        BitsBuilder classValuesBuilder,
        BitsBuilder offsetValuesBuilder,
        QuasiSuccinctBitsBuilder rankSamplesBuilder,
        QuasiSuccinctBitsBuilder offsetPositionSamplesBuilder,
        nuint rankSum)
    {
        var rankSamples = rankSamplesBuilder.Build();
        var offsetPositionSamples = offsetPositionSamplesBuilder.Build();
        var offsetValues = offsetValuesBuilder.Build();
        var classValues = classValuesBuilder.Build();
#if DEBUG
        CheckEncoding(
            blocksCount: blocksCount,
            bits: bits,
            offsetValues: offsetValues,
            classValues: classValues,
            offsetPositionSamples: offsetPositionSamples);
#endif
        return new SuccinctCompressedBits(
            size: (nuint)size,
            setBitsCount: rankSum,
            rankSamples: rankSamples,
            offsetPositionSamples: offsetPositionSamples,
            classValues: classValues,
            offsetValues: offsetValues);
    }

    public SuccinctCompressedBits Build()
    {
        var bits = _bits.Build();
        int size = (int)bits.Size;
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

        return BuildSuccinctBits(
            size: size,
            bits: bits,
            blocksCount: blocksCount,
            classValuesBuilder: classValuesBuilder,
            offsetValuesBuilder: offsetValuesBuilder,
            rankSamplesBuilder: rankSamplesBuilder,
            offsetPositionSamplesBuilder: offsetPositionSamplesBuilder,
            rankSum: rankSum);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IBitIndices IBitsBuilder.BuildBitIndices() => Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ISuccinctIndices IBitsBuilder.BuildSuccinctIndices() => Build();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ISuccinctCompressedIndices IBitsBuilder.BuildSuccinctCompressedIndices() => Build();

    public IBitIndices ClearAndBuildBitIndices(IBits bits)
    {
        ClearAndInitialize(bits);
        return Build();
    }

    public ISuccinctIndices ClearAndBuildSuccinctIndices(IBits bits)
    {
        ClearAndInitialize(bits);
        return Build();
    }

    public ISuccinctCompressedIndices ClearAndBuildSuccinctCompressedIndices(IBits bits)
    {
        ClearAndInitialize(bits);
        return Build();
    }
}
