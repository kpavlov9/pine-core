﻿using System.Numerics;

using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices;

namespace PineCore.tests.DataStructuresTests.SuccinctDataStructuresTests
{
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
        public void clear()
        {
            var bitsBuilder = new SuccinctCompressedBitsBuilder();

            Assert.True(bitsBuilder.Size == 0);

            bitsBuilder.Set(64);

            Assert.True(bitsBuilder.Size == 64 + 1);

            bitsBuilder.Clear();

            Assert.True(bitsBuilder.Size == 0);
        }


        [Fact]
        public void clear_and_build_compressed_succinct_indices_bits()
        {
        }

        [Fact]
        public void clear_and_build_compressed_succinct_indices_succinct()
        {
        }

        [Fact]
        public void clear_and_build_compressed_succinct_indices_succinct_compressed()
        {
        }

        [Fact]
        public void build_compressed_succinct_indices_succinct()
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

            var bits = bitsBuilder.BuildSuccinctCompressedIndices();

            Assert.Equal(bits.Size, bitsBuilder.Size);
            Assert.Equal(bits.UnsetBitsCount, bitsBuilder.UnsetBitsCount);
            Assert.Equal(bits.SetBitsCount, bitsBuilder.SetBitsCount);

            for (nuint i = 0; i < bitsLength; i++)
            {
                Assert.True(
                bits.GetBit(i) == bitsBuilder.GetBit(i)
                    , $"Failed at {i}.");
            }

            var expectedBits = bitsBuilder.Build();

            for (nuint i = 0; i < bitsLength; i++)
            {
                Assert.True(
                    bits.RankSetBits(i) == expectedBits.RankSetBits(i),
                    $"Failed at {i}.");
                Assert.True(
                    bits.RankUnsetBits(i) == expectedBits.RankUnsetBits(i),
                    $"Failed at {i}.");
            }

            for (nuint i = 0; i < expectedBits.SetBitsCount; i++)
            {
                var index1 = bits.SelectSetBits(i);
                var index2 = expectedBits.SelectSetBits(i);

                Assert.True(
                    index1 == index2, $"Failed at {i}: {index1} <=> {index2}.");
            }

            for (nuint i = 0; i < expectedBits.UnsetBitsCount; i++)
            {
                Assert.True(
                    bits.SelectUnsetBits(i) ==
                    expectedBits.SelectUnsetBits(i),
                    $"Failed at {i}.");
            }
        }


        [Fact]
        public void build_compressed_succinct_indices_bits()
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

            var bits = bitsBuilder.BuildSuccinctCompressedIndices();

            Assert.Equal(bits.Size, bitsBuilder.Size);
            Assert.Equal(bits.UnsetBitsCount, succinctBuilder.UnsetBitsCount);
            Assert.Equal(bits.SetBitsCount, succinctBuilder.SetBitsCount);

            for (nuint i = 0; i < bitsLength; i++)
            {
                Assert.True(
                bits.GetBit(i) == bitsBuilder.GetBit(i)
                    , $"Failed at {i}.");
            }

            var expectedBits = succinctBuilder.Build();

            for (nuint i = 0; i < bitsLength; i++)
            {
                Assert.True(
                    bits.RankSetBits(i) == expectedBits.RankSetBits(i),
                    $"Failed at {i}.");
                Assert.True(
                    bits.RankUnsetBits(i) == expectedBits.RankUnsetBits(i),
                    $"Failed at {i}.");
            }

            for (nuint i = 0; i < expectedBits.SetBitsCount; i++)
            {
                var index1 = bits.SelectSetBits(i);
                var index2 = expectedBits.SelectSetBits(i);

                Assert.True(
                    index1 == index2, $"Failed at {i}: {index1} <=> {index2}.");
            }

            for (nuint i = 0; i < expectedBits.UnsetBitsCount; i++)
            {
                Assert.True(
                    bits.SelectUnsetBits(i) ==
                    expectedBits.SelectUnsetBits(i),
                    $"Failed at {i}.");
            }
        }

        [Fact]
        public void build_compressed_succinct_indices_succinct_compressed()
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

            var bits = bitsBuilder.BuildSuccinctCompressedIndices();

            Assert.Equal(bits.Size, bitsBuilder.Size);
            Assert.Equal(bits.UnsetBitsCount, succinctBuilder.UnsetBitsCount);
            Assert.Equal(bits.SetBitsCount, succinctBuilder.SetBitsCount);

            for (nuint i = 0; i < bitsLength; i++)
            {
                Assert.True(
                bits.GetBit(i) == bitsBuilder.GetBit(i)
                    , $"Failed at {i}.");
            }

            var expectedBits = succinctBuilder.Build();

            for (nuint i = 0; i < bitsLength; i++)
            {
                Assert.True(
                    bits.RankSetBits(i) == expectedBits.RankSetBits(i),
                    $"Failed at {i}.");
                Assert.True(
                    bits.RankUnsetBits(i) == expectedBits.RankUnsetBits(i),
                    $"Failed at {i}.");
            }

            for (nuint i = 0; i < expectedBits.SetBitsCount; i++)
            {
                var index1 = bits.SelectSetBits(i);
                var index2 = expectedBits.SelectSetBits(i);

                Assert.True(
                    index1 == index2, $"Failed at {i}: {index1} <=> {index2}.");
            }

            for (nuint i = 0; i < expectedBits.UnsetBitsCount; i++)
            {
                Assert.True(
                    bits.SelectUnsetBits(i) ==
                    expectedBits.SelectUnsetBits(i),
                    $"Failed at {i}.");
            }
        }

        [Fact]
        public void offset()
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
        public void get()
        {
            var bitsBuilder = new BitsBuilder(65);
            bitsBuilder.AddSetBits(1);
            bitsBuilder.AddUnsetBits(63);
            bitsBuilder.AddSetBits(1);

            var compressedBitsBuilder = new SuccinctCompressedBitsBuilder(bitsBuilder);
            var compressedBits = compressedBitsBuilder.Build();

            var bits = bitsBuilder.Build();

            Assert.Equal(bits.Size, compressedBits.Size);
            for (nuint i = 0; i < 65; i++)
            {
                Assert.True(
                    bits.GetBit(i) == compressedBits.GetBit(i) &&
                    bits.GetBit(i) == compressedBitsBuilder.GetBit(i)
                    , $"Failed at {i}.");
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
                    bits.GetBit(i) == compressedBitsBuilder.GetBit(i)
                    , $"Failed at {i}.");
            }
        }


        [Fact]
        public void rank_bits()
        {
            var rand = new Random();
            nuint bitsLength = 10000 + (nuint)rand.Next() % 10000;
            var bitsBuilder = new BitsBuilder(bitsLength);
            var compressedBitsBuilder = new SuccinctCompressedBitsBuilder(bitsLength);
            for (uint i = 0; i < bitsLength; i++)
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
                    bits.RankSetBits(i) == compressedBits.RankSetBits(i),
                    $"Failed at {i}.");
                Assert.True(
                    bits.RankUnsetBits(i) == compressedBits.RankUnsetBits(i),
                    $"Failed at {i}.");
                Assert.True(
                    compressedBits.RankSetBits(i) == compressedBitsFromSize.RankSetBits(i),
                    $"Failed at {i}.");
                Assert.True(
                    compressedBits.RankUnsetBits(i) == compressedBitsFromSize.RankUnsetBits(i),
                    $"Failed at {i}.");
            }
        }

        [Fact]
        public void select_bits()
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

            for (uint i = 0; i <= values[values.Count - 1]; i++)
            {
                bits0Builder.Set(i);
            }

            foreach (uint i in values)
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


            values = new List<nuint>();
            var rand = new Random();
            nuint length = 10000 + (nuint)rand.Next() % 10000;
            var bitsBuilder2 = new BitsBuilder(length);
            var compressedBitsBuilder2 = new SuccinctCompressedBitsBuilder(length);

            for (uint i = 0; i < length; i++)
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

            for (uint i = 0; i < length; i++)
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
                
                Assert.True(
                    index1 == index2, $"Failed at {i}: {index1} <=> {index2}.");

                Assert.True(
                    index1 == index3, $"Failed at {i}: {index3} <=> {index3}.");
            }
            for (nuint i = 0; i < compressedBits.UnsetBitsCount; i++)
            {
                Assert.True(
                    bits.SelectUnsetBits(i) ==
                    compressedBits.SelectUnsetBits(i),
                    $"Failed at {i}.");

                Assert.True(
                    bits.SelectUnsetBits(i) ==
                    compressedBitsFromSize.SelectUnsetBits(i),
                    $"Failed at {i}.");
            }
        }

        [Fact]
        public void read_and_write()
        {
            var stream1 = new MemoryStream();
            var writer1 = new BinaryWriter(stream1);

            const nuint iterations = 100000;
            var bitsBuilder = new BitsBuilder(iterations);
            var compressedBitsBuilder = new SuccinctCompressedBitsBuilder(iterations);

            var rand = new Random();
            for (uint i = 0; i < iterations; i++)
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
    }
}
