using System.Numerics;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.Bits;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctBits;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;

namespace PineCore.tests.DataStructuresTests.SuccinctDataStructuresTests;

public class SuccinctCompressedBitsTests
{
    /// <summary>
    ///  Reference implementation.
    /// </summary>
    private static nuint CalculateClassCounts(nuint n, nuint k)
    {
        if (k > n - k)
        {
            k = n - k;
        }

        BigInteger classCount = 1;
        for (uint i = 0; i < k; i++)
        {
            classCount *= (ulong)(n - i);
            classCount /= i + 1;
        }

        return (nuint)classCount;
    }

    [Fact]
    public void static_initializer()
    {
        nuint[][] classCounts = SuccinctCompressedBitsBuilder.ClassCounts;
        for (var n = 0; n < classCounts.Length; n++)
        {
            for (var m = 0; m <= n; m++)
            {
                Assert.Equal(classCounts[n][m], CalculateClassCounts((nuint)n, (nuint)m));
            }
        }
    }

    [Fact]
    public void Clear()
    {
        var bitsBuilder = new SuccinctCompressedBitsBuilder();

        Assert.True(bitsBuilder.Size == 0);

        bitsBuilder.Set(64);

        Assert.True(bitsBuilder.Size == 64 + 1);

        bitsBuilder.Clear();

        Assert.True(bitsBuilder.Size == 0);
    }


    [Fact]
    public void clear_and_BuildCompressedSuccinctBits_indices_bits()
    {
    }

    [Fact]
    public void clear_and_BuildCompressedSuccinctBits_indices_succinct()
    {
    }

    [Fact]
    public void clear_and_BuildCompressedSuccinctBits_indices_succinct_compressed()
    {
    }

    [Fact]
    public void BuildCompressedSuccinctBits_indices_succinct()
    {
        const nuint bitsLength = 10000;
        var bitsBuilder = new SuccinctBitsBuilder(bitsLength);

        var rand = new Random();

        for (uint i = 0; i < bitsLength; i++)
        {
            if (rand.Next() % 2 != 0)
            {
                bitsBuilder.Set(i);
            }
            else
            {
                bitsBuilder.Unset(i);
            }
        }

        var bits = bitsBuilder.BuildSuccinctCompressedBits();

        Assert.Equal(bits.Size, bitsBuilder.Size);
        Assert.Equal(bits.UnsetBitsCount, bitsBuilder.UnsetBitsCount);
        Assert.Equal(bits.SetBitsCount, bitsBuilder.SetBitsCount);

        for (nuint i = 0; i < bitsLength; i++)
        {
            Assert.True(bits.GetBit(i) == bitsBuilder.GetBit(i));
        }

        var expectedBits = bitsBuilder.Build();

        for (nuint i = 0; i < bitsLength; i++)
        {
            Assert.True(
                bits.RankSetBits(i) == expectedBits.RankSetBits(i));
            Assert.True(
                bits.RankUnsetBits(i) == expectedBits.RankUnsetBits(i));
        }

        for (nuint i = 0; i < expectedBits.SetBitsCount; i++)
        {
            var index1 = bits.SelectSetBits(i);
            var index2 = expectedBits.SelectSetBits(i);

            Assert.True(index1 == index2);
        }

        for (nuint i = 0; i < expectedBits.UnsetBitsCount; i++)
        {
            Assert.True(
                bits.SelectUnsetBits(i) ==
                expectedBits.SelectUnsetBits(i));
        }
    }


    [Fact]
    public void BuildCompressedSuccinctBits_indices_bits()
    {
        const nuint bitsLength = 10000;
        var bitsBuilder = new BitsBuilder(bitsLength);
        var succinctBuilder = new SuccinctBitsBuilder(bitsLength);

        var rand = new Random();

        for (uint i = 0; i < bitsLength; i++)
        {
            if (rand.Next() % 2 != 0)
            {
                bitsBuilder.Set(i);
                succinctBuilder.Set(i);
            }
            else
            {
                bitsBuilder.Unset(i);
                succinctBuilder.Unset(i);
            }
        }

        var bits = bitsBuilder.BuildSuccinctCompressedBits();

        Assert.Equal(bits.Size, bitsBuilder.Size);
        Assert.Equal(bits.UnsetBitsCount, succinctBuilder.UnsetBitsCount);
        Assert.Equal(bits.SetBitsCount, succinctBuilder.SetBitsCount);

        for (nuint i = 0; i < bitsLength; i++)
        {
            Assert.True(
            bits.GetBit(i) == bitsBuilder.GetBit(i));
        }

        var expectedBits = succinctBuilder.Build();

        for (nuint i = 0; i < bitsLength; i++)
        {
            Assert.True(
                bits.RankSetBits(i) == expectedBits.RankSetBits(i));
            Assert.True(
                bits.RankUnsetBits(i) == expectedBits.RankUnsetBits(i));
        }

        for (nuint i = 0; i < expectedBits.SetBitsCount; i++)
        {
            var index1 = bits.SelectSetBits(i);
            var index2 = expectedBits.SelectSetBits(i);

            Assert.True(index1 == index2);
        }

        for (nuint i = 0; i < expectedBits.UnsetBitsCount; i++)
        {
            Assert.True(
                bits.SelectUnsetBits(i) ==
                expectedBits.SelectUnsetBits(i));
        }
    }

    [Fact]
    public void BuildCompressedSuccinctBits_indices_succinct_compressed()
    {
        const nuint bitsLength = 10000;
        var bitsBuilder = new SuccinctCompressedBitsBuilder(bitsLength);
        var succinctBuilder = new SuccinctBitsBuilder(bitsLength);

        var rand = new Random();

        for (uint i = 0; i < bitsLength; i++)
        {
            if (rand.Next() % 2 != 0)
            {
                bitsBuilder.Set(i);
                succinctBuilder.Set(i);
            }
            else
            {
                bitsBuilder.Unset(i);
                succinctBuilder.Unset(i);
            }
        }

        var bits = bitsBuilder.BuildSuccinctCompressedBits();

        Assert.Equal(bits.Size, bitsBuilder.Size);
        Assert.Equal(bitsLength, bits.Size);
        Assert.Equal(bits.UnsetBitsCount, succinctBuilder.UnsetBitsCount);
        Assert.Equal(bits.SetBitsCount, succinctBuilder.SetBitsCount);

        for (nuint i = 0; i < bitsLength; i++)
        {
            Assert.True(
            bits.GetBit(i) == bitsBuilder.GetBit(i));
        }

        var expectedBits = succinctBuilder.Build();

        for (nuint i = 0; i < bitsLength; i++)
        {
            Assert.True(
                bits.RankSetBits(i) == expectedBits.RankSetBits(i));
            Assert.True(
                bits.RankUnsetBits(i) == expectedBits.RankUnsetBits(i));
        }

        for (nuint i = 0; i < expectedBits.SetBitsCount; i++)
        {
            var index1 = bits.SelectSetBits(i);
            var index2 = expectedBits.SelectSetBits(i);

            Assert.True(
                index1 == index2);
        }

        for (nuint i = 0; i < expectedBits.UnsetBitsCount; i++)
        {
            Assert.True(
                bits.SelectUnsetBits(i) ==
                expectedBits.SelectUnsetBits(i));
        }
    }

    [Fact]
    public void OffsetOf()
    {
        nuint value = 1;
        var @class = (uint)BitOperations.PopCount(value);
        var block = SuccinctCompressedBitsBuilder.OffsetOf(value, @class);
        nuint value2 = SuccinctCompressedBits.InverseOffsetOf(block, @class);
        Assert.Equal(value, value2);

        value = 0;
        @class = (uint)BitOperations.PopCount(value);
        block = SuccinctCompressedBitsBuilder.OffsetOf(value, @class);
        value2 = SuccinctCompressedBits.InverseOffsetOf(block, @class);

        Assert.Equal(value, value2);

        var rand = new Random();
        for (int i = 0; i < 100000; i++)
        {
            // Randomly choose bits of length NativeBitCountMinusOne:
            value = (nuint)rand.Next() << 32;
            value |= (uint)rand.Next();
            // Assuming, that SuccinctCompressedBitsBuilder.BlockSize = NativeBitCountMinusOne,
            // take the least significant NativeBitCountMinusOne bits:
            value &= (NUIntOne << NativeBitCountMinusOne) - 1;

            @class = (uint)BitOperations.PopCount(value);
            block = SuccinctCompressedBitsBuilder.OffsetOf(value, @class);
            value2 = SuccinctCompressedBits.InverseOffsetOf(block, @class);
            Assert.Equal(value, value2);
        }
    }

    [Fact]
    public void Get()
    {
        var bitsBuilder = new BitsBuilder(65);
        bitsBuilder.AddSetBits(1);
        bitsBuilder.AddUnsetBits(63);
        bitsBuilder.AddSetBits(1);

        var compressedBitsBuilder = new SuccinctCompressedBitsBuilder(bitsBuilder);
        var compressedBits = compressedBitsBuilder.Build();

        var bits = bitsBuilder.Build();

        Assert.Equal(bits.Size, compressedBits.Size);
        Assert.Equal((nuint)65, compressedBits.Size);
        for (nuint i = 0; i < 65; i++)
        {
            Assert.True(
                bits.GetBit(i) == compressedBits.GetBit(i) &&
                bits.GetBit(i) == compressedBitsBuilder.GetBit(i));
        }

        const nuint bitsLength = 10000;
        bitsBuilder = new BitsBuilder(bitsLength);
        var rand = new Random();
        for (uint i = 0; i < bitsLength; i++)
        {
            if(rand.Next() % 2 != 0)
            {
                bitsBuilder.Set(i);
            }
            else
            {
                bitsBuilder.Unset(i);
            }
        }

        compressedBitsBuilder = new SuccinctCompressedBitsBuilder(bitsBuilder);
        compressedBits = compressedBitsBuilder.Build();
        bits = bitsBuilder.Build();

        Assert.Equal(bits.Size, compressedBits.Size);
        for (nuint i = 0; i < bitsLength; i++)
        {
            Assert.True(
                bits.GetBit(i) == compressedBits.GetBit(i) &&
                bits.GetBit(i) == compressedBitsBuilder.GetBit(i));
        }
    }


    [Fact]
    public void RankSetBits_RankUnsetBits()
    {
        var rand = new Random();
        nuint bitsLength = 10000 + (nuint)rand.Next() % 10000;
        var bitsBuilder = new BitsBuilder(bitsLength);
        var compressedBitsBuilder = new SuccinctCompressedBitsBuilder(bitsLength);
        for (nuint i = 0; i < bitsLength; i++)
        {
            if (rand.Next() % 2 != 0)
            {
                bitsBuilder.Set(i);
                compressedBitsBuilder.Set(i);
                Assert.True(compressedBitsBuilder.GetBit(i) == true);
            }
            else
            {
                bitsBuilder.Unset(i);
                compressedBitsBuilder.Unset(i);
                Assert.True(compressedBitsBuilder.GetBit(i) == false);
            }
        }

        var compressedBits = new SuccinctCompressedBitsBuilder(bitsBuilder).Build();
        var compressedBitsFromSize = compressedBitsBuilder.Build();
        var bits = new SuccinctBitsBuilder(bitsBuilder).Build();

        Assert.Equal(bits.Size, compressedBits.Size);
        Assert.Equal(bits.Size, compressedBitsFromSize.Size);

        Assert.Equal(bits.UnsetBitsCount, compressedBits.UnsetBitsCount);
        Assert.Equal(bits.UnsetBitsCount, compressedBitsFromSize.UnsetBitsCount);

        Assert.Equal(bits.SetBitsCount, compressedBits.SetBitsCount);
        Assert.Equal(bits.SetBitsCount, compressedBitsFromSize.SetBitsCount);

        for (nuint i = 0; i < bitsLength; i++)
        {
            Assert.True(
                bits.RankSetBits(i) == compressedBits.RankSetBits(i));
            Assert.True(
                bits.RankUnsetBits(i) == compressedBits.RankUnsetBits(i));
            Assert.True(
                compressedBits.RankSetBits(i) == compressedBitsFromSize.RankSetBits(i));
            Assert.True(
                compressedBits.RankUnsetBits(i) == compressedBitsFromSize.RankUnsetBits(i));
        }
    }

   [Fact]
    public void SelectSetBits_simple()
    {
        var bits1Builder = new BitsBuilder(87);

        for (nuint i = 0; i < 87; i++)
        {
            if(i == 0 || i == 1 || i == 29 || i == 30)
            {
                bits1Builder.Set(i);
            }
        }

        var compressedBits1 = new SuccinctCompressedBitsBuilder(bits1Builder).Build();

        Assert.Equal((nuint)0, compressedBits1.SelectSetBits(0));
        Assert.Equal((nuint)1, compressedBits1.SelectSetBits(1));

        Assert.Equal((nuint)29, compressedBits1.SelectSetBits(2));
        Assert.Equal((nuint)30, compressedBits1.SelectSetBits(3));

        Assert.Equal(bits1Builder.Size, compressedBits1.SelectSetBits(4));
    }

    [Fact]
    public void SelectSetBits_SelectUnsetBits()
    {
        var values = new List<nuint>
        {
            0,
            511,
            512,
            1000,
            2000,
            2015,
            2016,
            2017,
            3000
        };

        var bits1Builder = new BitsBuilder(values.Max());
        var bits0Builder = new BitsBuilder(values.Max());

        bits0Builder.AddSetBits((int)values[^1]+1);

        foreach (nuint i in values)
        {
            bits1Builder.Set(i);

            bits0Builder.Unset(i);
        }

        var compressedBits1 = new SuccinctCompressedBitsBuilder(bits1Builder).Build();
        var compressedBits0 = new SuccinctCompressedBitsBuilder(bits0Builder).Build();

        nuint counter = 0;

        foreach (nuint v in values)
        {
            Assert.Equal(v, compressedBits1.SelectSetBits(counter));
            Assert.Equal(v, compressedBits0.SelectUnsetBits(counter));
            counter++;
        }


        values = [];
        var rand = new Random();
        nuint length = 10000 + (nuint)rand.Next() % 10000;
        var bitsBuilder2 = new BitsBuilder(length);
        var compressedBitsBuilder2 = new SuccinctCompressedBitsBuilder(length);

        for (nuint i = 0; i < length; i++)
        {
            bool b = rand.Next() % 2 != 0;
            if (b)
            {
                values.Add(i);
                bitsBuilder2.Set(i);
                compressedBitsBuilder2.Set(i);
                Assert.True(compressedBitsBuilder2.GetBit(i) == true);
            }
            else
            {
                bitsBuilder2.Unset(i);
                compressedBitsBuilder2.Unset(i);
                Assert.True(compressedBitsBuilder2.GetBit(i) == false);
            }
            
        }
        var compressed2 = new SuccinctCompressedBitsBuilder(bitsBuilder2).Build();
        var compressed2FromSize = compressedBitsBuilder2.Build();

        counter = 0;
        foreach (nuint v in values)
        {
            Assert.Equal(v, compressed2.SelectSetBits(counter));
            Assert.Equal(v, compressed2FromSize.SelectSetBits(counter));
            counter++;
        }

        var bitsBuilder = new BitsBuilder(length);
        var compressedBitsBuilder = new SuccinctCompressedBitsBuilder(length);

        for (nuint i = 0; i < length; i++)
        {
            if(rand.Next() % 2 != 0)
            {
                bitsBuilder.Set(i);
                compressedBitsBuilder.Set(i);
                Assert.True(compressedBitsBuilder.GetBit(i) == true);
            }
            else
            {
                bitsBuilder.Unset(i);
                compressedBitsBuilder.Unset(i);
                Assert.True(compressedBitsBuilder.GetBit(i) == false);
            }
        }

        var compressedBits = new SuccinctCompressedBitsBuilder(bitsBuilder).Build();
        var compressedBitsFromSize = compressedBitsBuilder.Build();
        var bits = new SuccinctBitsBuilder(bitsBuilder).Build();

        Assert.Equal(bits.SetBitsCount, compressedBits.SetBitsCount);
        Assert.Equal(bits.UnsetBitsCount, compressedBits.UnsetBitsCount);

        Assert.Equal(bits.SetBitsCount, compressedBitsFromSize.SetBitsCount);
        Assert.Equal(bits.UnsetBitsCount, compressedBitsFromSize.UnsetBitsCount);

        Assert.Equal(compressedBits.Size, compressedBitsFromSize.Size);

        for (nuint i = 0; i < compressedBits.SetBitsCount; i++)
        {
            var index1 = bits.SelectSetBits(i);
            var index2 = compressedBits.SelectSetBits(i);
            var index3 = compressedBitsFromSize.SelectSetBits(i);
            
            Assert.True(index1 == index2);

            Assert.True(index1 == index3);
        }
        for (nuint i = 0; i < compressedBits.UnsetBitsCount; i++)
        {
            Assert.True(
                bits.SelectUnsetBits(i) ==
                compressedBits.SelectUnsetBits(i));

            Assert.True(
                bits.SelectUnsetBits(i) ==
                compressedBitsFromSize.SelectUnsetBits(i));
        }
    }

    [Fact]
    public void Read_and_Write()
    {
        var stream1 = new MemoryStream();
        var writer1 = new BinaryWriter(stream1);

        const nuint iterations = 100000;
        var bitsBuilder = new BitsBuilder(iterations);
        var compressedBitsBuilder = new SuccinctCompressedBitsBuilder(iterations);

        var rand = new Random();
        for (nuint i = 0; i < iterations; i++)
        {
            if (rand.Next() % 2 != 0)
            {
                bitsBuilder.Set(i);
                compressedBitsBuilder.Set(i);
                Assert.True(compressedBitsBuilder.GetBit(i) == true);
            }
            else
            {
                bitsBuilder.Unset(i);
                compressedBitsBuilder.Unset(i);
                Assert.True(compressedBitsBuilder.GetBit(i) == false);
            }
        }

        compressedBitsBuilder = new SuccinctCompressedBitsBuilder(bitsBuilder);
        var compressedBits = compressedBitsBuilder.Build();
        var compressedBitsFromSize = compressedBitsBuilder.Build();

        compressedBits.Write(writer1);
        compressedBitsFromSize.Write(writer1);

        var bits = bitsBuilder.Build();

        stream1.Seek(0, SeekOrigin.Begin);
        var compressedBits2 = SuccinctCompressedBits.Read(new BinaryReader(stream1));

        Assert.Equal(compressedBits.Size, compressedBits2.Size);
        for (nuint i = 0; i < compressedBits.Size; i++)
        {
            Assert.Equal(compressedBitsBuilder.GetBit(i), bitsBuilder.GetBit(i));
            Assert.Equal(compressedBits.GetBit(i), bits.GetBit(i));
            Assert.Equal(compressedBits.GetBit(i), compressedBits2.GetBit(i));
            Assert.Equal(compressedBitsFromSize.GetBit(i), bits.GetBit(i));
            Assert.Equal(compressedBitsFromSize.GetBit(i), compressedBits2.GetBit(i));
        }

        Assert.Equal(compressedBits, compressedBits2);
        Assert.Equal(compressedBitsFromSize, compressedBits2);
    }

    [Fact]
    public void RankSetBits_during_building_incremental_appends()
    {
        // Test that rank works correctly during building with incremental appends
        var builder = new SuccinctCompressedBitsBuilder(5000);
        var reference = new BitsBuilder(5000);
        var rand = new Random(42);

        for (nuint i = 0; i < 5000; i++)
        {
            bool bit = rand.Next() % 2 != 0;
            if (bit)
            {
                builder.Set(i);
                reference.Set(i);
            }
            else
            {
                builder.Unset(i);
                reference.Unset(i);
            }

            // Test rank at every 100th position during building
            if (i % 100 == 0 && i > 0)
            {
                nuint builderRank = builder.RankSetBits(i);
                
                // Calculate reference rank manually
                nuint expectedRank = 0;
                for (nuint j = 0; j < i; j++)
                {
                    if (reference.GetBit(j))
                        expectedRank++;
                }

                Assert.Equal(expectedRank, builderRank);
            }
        }
    }

    [Fact]
    public void RankSetBits_during_building_vs_after_build()
    {
        // Verify rank queries during building match rank after Build()
        var builder = new SuccinctCompressedBitsBuilder(10000);
        var rand = new Random(123);

        for (nuint i = 0; i < 10000; i++)
        {
            if (rand.Next() % 2 != 0)
                builder.Set(i);
        }

        // Capture ranks during building
        var ranksDuringBuild = new Dictionary<nuint, nuint>();
        var testPositions = new List<nuint> { 0, 100, 500, 1000, 2016, 2017, 5000, 9999, 10000 };
        
        foreach (var pos in testPositions)
        {
            ranksDuringBuild[pos] = builder.RankSetBits(pos);
        }

        // Build and compare ranks
        var compressed = builder.Build();
        
        foreach (var pos in testPositions)
        {
            Assert.Equal(ranksDuringBuild[pos], compressed.RankSetBits(pos));
        }
    }

    [Fact]
    public void RankSetBits_at_super_block_boundaries()
    {
        // Test rank at super-block boundaries (every 2016 bits)
        const nuint SuperBlockSize = 2016; // 63 * 32
        var builder = new SuccinctCompressedBitsBuilder(SuperBlockSize * 5);
        
        // Set every other bit
        for (nuint i = 0; i < SuperBlockSize * 5; i += 2)
        {
            builder.Set(i);
        }

        // Test rank at super-block boundaries
        for (nuint sb = 0; sb <= 5; sb++)
        {
            nuint position = sb * SuperBlockSize;
            nuint rank = builder.RankSetBits(position);
            nuint expectedRank = position / 2; // Every other bit is set
            
            Assert.Equal(expectedRank, rank);
        }
    }

    [Fact]
    public void RankSetBits_with_middle_modifications()
    {
        // Test that modifying bits in the middle triggers rank recomputation
        var builder = new SuccinctCompressedBitsBuilder(5000);
        
        // Set all bits to 1
        for (nuint i = 0; i < 5000; i++)
        {
            builder.Set(i);
        }

        // Verify rank
        Assert.Equal((nuint)2500, builder.RankSetBits(2500));
        Assert.Equal((nuint)5000, builder.RankSetBits(5000));

        // Unset some bits in the middle
        for (nuint i = 1000; i < 2000; i++)
        {
            builder.Unset(i);
        }

        // Verify rank updated correctly
        Assert.Equal((nuint)1000, builder.RankSetBits(1000));
        Assert.Equal((nuint)1000, builder.RankSetBits(1500)); // 1000 before, 0 in range
        Assert.Equal((nuint)1000, builder.RankSetBits(2000)); // Still 1000
        Assert.Equal((nuint)1500, builder.RankSetBits(2500)); // 1000 + 500 more after gap
    }

    [Fact]
    public void RankUnsetBits()
    {
        var builder = new SuccinctCompressedBitsBuilder(1000);
        
        // Set every 3rd bit
        for (nuint i = 0; i < 1000; i++)
        {
            if (i % 3 == 0)
                builder.Set(i);
        }

        // Test RankUnsetBits
        for (nuint i = 0; i <= 1000; i += 100)
        {
            nuint rankSet = builder.RankSetBits(i);
            nuint rankUnset = builder.RankUnsetBits(i);
            
            Assert.Equal(i, rankSet + rankUnset);
        }
    }

    [Fact]
    public void RankSetBits_empty_builder()
    {
        var builder = new SuccinctCompressedBitsBuilder();
        
        // Rank on empty builder should be 0
        Assert.Equal((nuint)0, builder.RankSetBits(0));
        
        builder.Set(10);
        Assert.Equal((nuint)0, builder.RankSetBits(10));
        Assert.Equal((nuint)1, builder.RankSetBits(11));
    }

    [Fact]
    public void RankSetBits_dense_pattern()
    {
        // Test with all bits set
        var builder = new SuccinctCompressedBitsBuilder(10000);
        
        for (nuint i = 0; i < 10000; i++)
        {
            builder.Set(i);
        }

        // Rank should equal position for all 1s
        for (nuint i = 0; i <= 10000; i += 500)
        {
            Assert.Equal(i, builder.RankSetBits(i));
        }
    }

    [Fact]
    public void RankSetBits_sparse_pattern()
    {
        // Test with very sparse bits
        var builder = new SuccinctCompressedBitsBuilder(100000);
        var setBits = new List<nuint> { 10, 1000, 5000, 50000, 99999 };
        
        foreach (var bit in setBits)
        {
            builder.Set(bit);
        }

        // Verify ranks
        Assert.Equal((nuint)0, builder.RankSetBits(10));
        Assert.Equal((nuint)1, builder.RankSetBits(11));
        Assert.Equal((nuint)1, builder.RankSetBits(1000));
        Assert.Equal((nuint)2, builder.RankSetBits(1001));
        Assert.Equal((nuint)2, builder.RankSetBits(5000));
        Assert.Equal((nuint)3, builder.RankSetBits(5001));
        Assert.Equal((nuint)5, builder.RankSetBits(100000));
    }

    [Fact]
    public void RankSetBits_with_push_operations()
    {
        var builder = new SuccinctCompressedBitsBuilder();
        
        // Add bits using PushOnes and PushZeroes
        builder.PushOnes(1000);    // Positions 0-999 set
        builder.PushZeroes(1000);  // Positions 1000-1999 unset
        builder.PushOnes(1000);    // Positions 2000-2999 set

        Assert.Equal((nuint)1000, builder.RankSetBits(1000));
        Assert.Equal((nuint)1000, builder.RankSetBits(2000));
        Assert.Equal((nuint)2000, builder.RankSetBits(3000));
    }

    [Fact]
    public void RankSetBits_after_clear()
    {
        var builder = new SuccinctCompressedBitsBuilder(1000);
        
        for (nuint i = 0; i < 1000; i++)
        {
            builder.Set(i);
        }

        Assert.Equal((nuint)500, builder.RankSetBits(500));

        builder.Clear();
        
        Assert.Equal((nuint)0, builder.RankSetBits(0));
        
        builder.Set(100);
        Assert.Equal((nuint)0, builder.RankSetBits(100));
        Assert.Equal((nuint)1, builder.RankSetBits(101));
    }

    [Fact]
    public void rank_consistency_with_original_builder()
    {
        // Verify that SuccinctCompressedBitsBuilder produces same results as original
        const nuint size = 10000;
        var rand = new Random(999);
        
        var builderWithRank = new SuccinctCompressedBitsBuilder(size);
        var originalBuilder = new SuccinctCompressedBitsBuilder(size);
        
        for (nuint i = 0; i < size; i++)
        {
            bool bit = rand.Next() % 2 != 0;
            if (bit)
            {
                builderWithRank.Set(i);
                originalBuilder.Set(i);
            }
        }

        var compressedWithRank = builderWithRank.Build();
        var compressedOriginal = originalBuilder.Build();

        // Compare all properties
        Assert.Equal(compressedOriginal.Size, compressedWithRank.Size);
        Assert.Equal(compressedOriginal.SetBitsCount, compressedWithRank.SetBitsCount);
        Assert.Equal(compressedOriginal.UnsetBitsCount, compressedWithRank.UnsetBitsCount);

        // Compare GetBit
        for (nuint i = 0; i < size; i++)
        {
            Assert.Equal(compressedOriginal.GetBit(i), compressedWithRank.GetBit(i));
        }

        // Compare Rank
        // Note: Valid rank positions are [0, size), not [0, size]
        for (nuint i = 0; i < size; i += 100)
        {
            Assert.Equal(compressedOriginal.RankSetBits(i), compressedWithRank.RankSetBits(i));
            Assert.Equal(compressedOriginal.RankUnsetBits(i), compressedWithRank.RankUnsetBits(i));
        }

        // Compare Select
        for (nuint i = 0; i < compressedOriginal.SetBitsCount; i += 10)
        {
            Assert.Equal(compressedOriginal.SelectSetBits(i), compressedWithRank.SelectSetBits(i));
        }
    }

    [Fact]
    public void RankSetBits_stress_test_large_size()
    {
        // Test with large bit array
        const nuint size = 100000;
        var builder = new SuccinctCompressedBitsBuilder(size);
        var rand = new Random(777);
        
        // Set random bits
        var setBitsList = new List<nuint>();
        for (nuint i = 0; i < size; i++)
        {
            if (rand.Next() % 10 == 0) // 10% density
            {
                builder.Set(i);
                setBitsList.Add(i);
            }
        }

        // Verify rank at random positions
        for (int test = 0; test < 100; test++)
        {
            nuint position = (nuint)(rand.Next() % (int)size);
            nuint rank = builder.RankSetBits(position);
            
            // Calculate expected rank
            nuint expected = 0;
            foreach (var bit in setBitsList)
            {
                if (bit < position)
                    expected++;
            }
            
            Assert.Equal(expected, rank);
        }
    }

    [Fact]
    public void RankSetBits_boundary_conditions()
    {
        var builder = new SuccinctCompressedBitsBuilder(100);
        
        builder.Set(0);
        builder.Set(50);
        builder.Set(99);

        // Test boundary conditions
        Assert.Equal((nuint)0, builder.RankSetBits(0));   // Before first bit
        Assert.Equal((nuint)1, builder.RankSetBits(1));   // After first bit
        Assert.Equal((nuint)1, builder.RankSetBits(50));  // At second bit
        Assert.Equal((nuint)2, builder.RankSetBits(51));  // After second bit
        Assert.Equal((nuint)2, builder.RankSetBits(99));  // At last bit
        Assert.Equal((nuint)3, builder.RankSetBits(100)); // After last bit
    }

    [Fact]
    public void rank_with_add_operations()
    {
        var builder = new SuccinctCompressedBitsBuilder();
        
        // Test Add(bool)
        for (int i = 0; i < 100; i++)
        {
            builder.Add(i % 2 == 0);
        }

        Assert.Equal((nuint)50, builder.RankSetBits(100)); // 50 set bits (even positions)
        Assert.Equal((nuint)50, builder.RankUnsetBits(100)); // 50 unset bits (odd positions)
    }

    [Fact]
    public void rank_matches_succinct_bits()
    {
        // Verify rank matches SuccinctBits implementation
        // Use size that's a multiple of BlockSize (63) to avoid partial blocks
        const nuint size = 63 * 80; // 5040 - evenly divisible by 63
        var bitsBuilder = new BitsBuilder(size);
        var rand = new Random(456);
        
        for (nuint i = 0; i < size; i++)
        {
            if (rand.Next() % 2 != 0)
                bitsBuilder.Set(i);
        }

        var succinctBits = new SuccinctBitsBuilder(bitsBuilder).Build();
        var compressedBuilderWithRank = new SuccinctCompressedBitsBuilder(bitsBuilder);

        // Compare rank during building
        // Note: rank at position i counts bits [0, i), so valid positions are [0, size]
        // However, some implementations may validate position < size, so we test up to size-1
        for (nuint i = 0; i < size; i += 250)
        {
            Assert.Equal(succinctBits.RankSetBits(i), compressedBuilderWithRank.RankSetBits(i));
            Assert.Equal(succinctBits.RankUnsetBits(i), compressedBuilderWithRank.RankUnsetBits(i));
        }

        // Compare after building
        var compressedWithRank = compressedBuilderWithRank.Build();
        for (nuint i = 0; i < size; i += 250)
        {
            Assert.Equal(succinctBits.RankSetBits(i), compressedWithRank.RankSetBits(i));
        }
    }

    [Fact]
    public void RankSetBits_alternating_pattern()
    {
        var builder = new SuccinctCompressedBitsBuilder(10000);
        
        // Alternating pattern: 101010...
        for (nuint i = 0; i < 10000; i++)
        {
            if (i % 2 == 0)
                builder.Set(i);
        }

        // Rank should be position / 2
        for (nuint i = 0; i <= 10000; i += 500)
        {
            nuint expectedRank = i / 2;
            Assert.Equal(expectedRank, builder.RankSetBits(i));
        }
    }

    [Fact]
    public void RankSetBits_block_aligned_positions()
    {
        // Test rank at positions aligned with block size (63 bits)
        const nuint blockSize = 63;
        var builder = new SuccinctCompressedBitsBuilder(blockSize * 100);
        
        // Set first bit of each block
        for (nuint block = 0; block < 100; block++)
        {
            builder.Set(block * blockSize);
        }

        // Test rank at block boundaries
        for (nuint block = 0; block <= 100; block++)
        {
            nuint position = block * blockSize;
            Assert.Equal(block, builder.RankSetBits(position));
        }
    }

    /// <summary>
    /// Quick verification tests for the rank fixes
    /// </summary>
    public class RankVerificationTests
    {
        [Fact]
        public void simple_RankSetBits_test()
        {
            var builder = new SuccinctCompressedBitsBuilder(100);
            
            builder.Set(10);
            builder.Set(50);
            builder.Set(90);
            
            // Rank should be: 0 bits before 10, 1 before 11, 1 before 50, 2 before 51, etc.
            Assert.Equal((nuint)0, builder.RankSetBits(10));
            Assert.Equal((nuint)1, builder.RankSetBits(11));
            Assert.Equal((nuint)1, builder.RankSetBits(50));
            Assert.Equal((nuint)2, builder.RankSetBits(51));
            Assert.Equal((nuint)2, builder.RankSetBits(90));
            Assert.Equal((nuint)3, builder.RankSetBits(91));
            Assert.Equal((nuint)3, builder.RankSetBits(100));
        }

        [Fact]
        public void push_operations_test()
        {
            var builder = new SuccinctCompressedBitsBuilder();
            
            builder.PushOnes(1000);    // Positions 0-999 set
            builder.PushZeroes(1000);  // Positions 1000-1999 unset
            builder.PushOnes(1000);    // Positions 2000-2999 set

            // Test ranks
            Assert.Equal((nuint)0, builder.RankSetBits(0));
            Assert.Equal((nuint)500, builder.RankSetBits(500));
            Assert.Equal((nuint)1000, builder.RankSetBits(1000));
            Assert.Equal((nuint)1000, builder.RankSetBits(1500));
            Assert.Equal((nuint)1000, builder.RankSetBits(2000));
            Assert.Equal((nuint)1500, builder.RankSetBits(2500));
            Assert.Equal((nuint)2000, builder.RankSetBits(3000));
        }

        [Fact]
        public void super_block_boundary_test()
        {
            const nuint SuperBlockSize = 2016;
            var builder = new SuccinctCompressedBitsBuilder(SuperBlockSize * 3);
            
            // Set every 10th bit
            for (nuint i = 0; i < SuperBlockSize * 3; i += 10)
            {
                builder.Set(i);
            }

            // Test at boundaries
            nuint expectedRank0 = 0;
            Assert.Equal(expectedRank0, builder.RankSetBits(0));
            
            nuint actualRank2016 = builder.RankSetBits(SuperBlockSize);
            Assert.True(actualRank2016 >= 201 && actualRank2016 <= 202, 
                $"Expected ~201-202, got {actualRank2016}");
        }

        [Fact]
        public void incremental_with_reference()
        {
            const int size = 1000;
            var builder = new SuccinctCompressedBitsBuilder(size);
            var reference = new BitsBuilder(size);
            var rand = new Random(42);

            // Set random bits
            for (nuint i = 0; i < size; i++)
            {
                bool bit = rand.Next() % 2 != 0;
                if (bit)
                {
                    builder.Set(i);
                    reference.Set(i);
                }
            }

            // Calculate reference rank manually
            nuint CalculateReferenceRank(nuint position)
            {
                nuint count = 0;
                for (nuint j = 0; j < position; j++)
                {
                    if (reference.GetBit(j))
                        count++;
                }
                return count;
            }

            // Test at various positions
            for (nuint testPos = 0; testPos <= size; testPos += 100)
            {
                nuint expected = CalculateReferenceRank(testPos);
                nuint actual = builder.RankSetBits(testPos);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void RankSetBits_sample_semantics()
        {
            // This test verifies the semantics: _rankSamples[i] = count before position i*SuperBlockSize
            const nuint SuperBlockSize = 2016;
            var builder = new SuccinctCompressedBitsBuilder(SuperBlockSize * 2);
            
            // Set first half of first super-block
            for (nuint i = 0; i < SuperBlockSize / 2; i++)
            {
                builder.Set(i);
            }
            
            // Set second half of second super-block
            for (nuint i = SuperBlockSize + SuperBlockSize / 2; i < SuperBlockSize * 2; i++)
            {
                builder.Set(i);
            }

            // Rank at position 0 should be 0
            Assert.Equal((nuint)0, builder.RankSetBits(0));
            
            // Rank at position SuperBlockSize should be ~1008 (half of first super-block)
            nuint rankAtSB1 = builder.RankSetBits(SuperBlockSize);
            Assert.Equal(SuperBlockSize / 2, rankAtSB1);
            
            // Rank at position 2*SuperBlockSize should still be ~1008 (second half wasn't set yet)
            nuint rankAtSB2 = builder.RankSetBits(SuperBlockSize * 2);
            Assert.Equal(SuperBlockSize / 2 + SuperBlockSize / 2, rankAtSB2);
        }

        [Fact]
        public void RankSetBits_beyond_size()
        {
            // Test querying rank at positions beyond the actual size
            var builder = new SuccinctCompressedBitsBuilder();
            
            // Set some bits
            for (nuint i = 0; i < 1000; i += 10)
            {
                builder.Set(i);
            }
            
            // Query at actual size
            nuint sizeRank = builder.RankSetBits(builder.Size);
            
            // Query beyond size (should clip to size)
            nuint beyondRank = builder.RankSetBits(builder.Size + 1000);
            
            Assert.Equal(sizeRank, beyondRank);
        }

        [Fact]
        public void RankSetBits_with_bits_builder_copy()
        {
            // This reproduces the exact scenario from rank_matches_succinct_bits test
            const nuint size = 5000;
            var bitsBuilder = new BitsBuilder(size);
            var rand = new Random(456);
            
            for (nuint i = 0; i < size; i++)
            {
                if (rand.Next() % 2 != 0)
                    bitsBuilder.Set(i);
            }

            // This should not throw
            var compressedBuilderWithRank = new SuccinctCompressedBitsBuilder(bitsBuilder);
            
            // Query at various positions including the size
            for (nuint i = 0; i <= size; i += 250)
            {
                var rank = compressedBuilderWithRank.RankSetBits(i); // Should not throw
                Assert.True(rank <= i); // Rank can't exceed position
            }
        }
    }
}
