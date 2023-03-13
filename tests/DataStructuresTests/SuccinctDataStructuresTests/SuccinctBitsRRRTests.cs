using System.Numerics;

using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices;

namespace PineCore.tests.DataStructuresTests.SuccinctDataStructuresTests
{
    public class SuccinctBitsRRRTests
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
            nuint[][] classCounts = SuccinctBitsRRRBuilder.ClassCounts;
            for (var n = 0; n < classCounts.Length; n++)
            {
                for (var m = 0; m <= n; m++)
                {
                    Assert.Equal(classCounts[n][m], CalculateClassCounts((nuint)n, (nuint)m));
                }
            }
        }

        [Fact]
        public void offset()
        {
            nuint value = 1;
            var @class = (uint)BitOperations.PopCount(value);
            var block = SuccinctBitsRRRBuilder.OffsetOf(value, @class);
            nuint value2 = SuccinctBitsRRR.InverseOffsetOf(block, @class);
            Assert.Equal(value, value2);

            value = 0;
            @class = (uint)BitOperations.PopCount(value);
            block = SuccinctBitsRRRBuilder.OffsetOf(value, @class);
            value2 = SuccinctBitsRRR.InverseOffsetOf(block, @class);

            Assert.Equal(value, value2);

            var rand = new Random();
            for (int i = 0; i < 100000; i++)
            {
                // Randomly choose bits of length NativeBitCountMinusOne:
                value = (nuint)rand.Next() << 32;
                value |= (uint)rand.Next();
                // Assuming that RRBitVector.BlockSize = NativeBitCountMinusOne,
                // take the least significant NativeBitCountMinusOne bits:
                value &= (NUIntOne << NativeBitCountMinusOne) - 1;

                @class = (uint)BitOperations.PopCount(value);
                block = SuccinctBitsRRRBuilder.OffsetOf(value, @class);
                value2 = SuccinctBitsRRR.InverseOffsetOf(block, @class);
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

            var rrrBits = new SuccinctBitsRRRBuilder(bitsBuilder).Build();

            var bits = bitsBuilder.Build();

            Assert.Equal(bits.Size, rrrBits.Size);
            for (nuint i = 0; i < 65; i++)
            {
                Assert.True(bits.GetBit(i) == rrrBits.GetBit(i), $"Failed at {i}.");
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

            rrrBits = new SuccinctBitsRRRBuilder(bitsBuilder).Build();
            bits = bitsBuilder.Build();

            Assert.Equal(bits.Size, rrrBits.Size);
            for (nuint i = 0; i < bitsLength; i++)
            {
                Assert.True(bits.GetBit(i) == rrrBits.GetBit(i), $"Failed at {i}.");
            }
        }


        [Fact]
        public void rank_bits()
        {
            var rand = new Random();
            nuint bitsLength = 10000 + (nuint)rand.Next() % 10000;
            var bitsBuilder = new BitsBuilder(bitsLength);
            var rrrBitsBuilder = new SuccinctBitsRRRBuilder(bitsLength);
            for (uint i = 0; i < bitsLength; i++)
            {
                if (rand.Next() % 2 != 0)
                {
                    bitsBuilder.Set(i);
                    rrrBitsBuilder.Set(i);
                }
                else
                {
                    bitsBuilder.Unset(i);
                    rrrBitsBuilder.Unset(i);
                }
            }

            var rrrBits = new SuccinctBitsRRRBuilder(bitsBuilder).Build();
            var rrBitsFromSize = rrrBitsBuilder.Build();
            var bits = new SuccinctBitsBuilder(bitsBuilder).Build();
            Assert.Equal(bits.Size, rrrBits.Size);
            Assert.Equal(bits.Size, rrBitsFromSize.Size);

            for (nuint i = 0; i < bitsLength; i++)
            {
                Assert.True(
                    bits.RankSetBits(i) == rrrBits.RankSetBits(i),
                    $"Failed at {i}.");
                Assert.True(
                    bits.RankUnsetBits(i) == rrrBits.RankUnsetBits(i),
                    $"Failed at {i}.");
                Assert.True(
                    rrrBits.RankSetBits(i) == rrBitsFromSize.RankSetBits(i),
                    $"Failed at {i}.");
                Assert.True(
                    rrrBits.RankUnsetBits(i) == rrBitsFromSize.RankUnsetBits(i),
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

            var rrrBits1 = new SuccinctBitsRRRBuilder(bits1Builder).Build();
            var rrrBits0 = new SuccinctBitsRRRBuilder(bits0Builder).Build();

            nuint counter = 0;

            foreach (nuint v in values)
            {
                Assert.Equal(v, rrrBits1.SelectSetBits(counter));
                Assert.Equal(v, rrrBits0.SelectUnsetBits(counter));
                counter++;
            }


            values = new List<nuint>();
            var rand = new Random();
            nuint length = 10000 + (nuint)rand.Next() % 10000;
            var bitsBuilder2 = new BitsBuilder(length);
            var rrrBitsBuilder2 = new SuccinctBitsRRRBuilder(length);

            for (uint i = 0; i < length; i++)
            {
                bool b = rand.Next() % 2 != 0;
                if (b)
                {
                    values.Add(i);
                    bitsBuilder2.Set(i);
                    rrrBitsBuilder2.Set(i);
                }
                else
                {
                    bitsBuilder2.Unset(i);
                    rrrBitsBuilder2.Unset(i);
                }
                
            }
            var rrr2 = new SuccinctBitsRRRBuilder(bitsBuilder2).Build();
            var rrr2FromSize = rrrBitsBuilder2.Build();

            counter = 0;
            foreach (nuint v in values)
            {
                Assert.Equal(v, rrr2.SelectSetBits(counter));
                Assert.Equal(v, rrr2FromSize.SelectSetBits(counter));
                counter++;
            }

            var bitsBuilder = new BitsBuilder(length);
            var rrrBitsBuilder = new SuccinctBitsRRRBuilder(length);

            for (uint i = 0; i < length; i++)
            {
                if(rand.Next() % 2 != 0)
                {
                    bitsBuilder.Set(i);
                    rrrBitsBuilder.Set(i);
                }
                else
                {
                    bitsBuilder.Unset(i);
                    rrrBitsBuilder.Unset(i);
                }
            }

            var rrrBits = new SuccinctBitsRRRBuilder(bitsBuilder).Build();
            var rrrBitsFromSize = rrrBitsBuilder.Build();
            var bits = new SuccinctBitsBuilder(bitsBuilder).Build();

            Assert.Equal(bits.SetBitsCount, rrrBits.SetBitsCount);
            Assert.Equal(bits.UnsetBitsCount, rrrBits.UnsetBitsCount);

            Assert.Equal(bits.SetBitsCount, rrrBitsFromSize.SetBitsCount);
            Assert.Equal(bits.UnsetBitsCount, rrrBitsFromSize.UnsetBitsCount);

            Assert.Equal(rrrBits.Size, rrrBitsFromSize.Size);

            for (nuint i = 0; i < rrrBits.SetBitsCount; i++)
            {
                var index1 = bits.SelectSetBits(i);
                var index2 = rrrBits.SelectSetBits(i);
                var index3 = rrrBitsFromSize.SelectSetBits(i);
                
                Assert.True(
                    index1 == index2, $"Failed at {i}: {index1} <=> {index2}.");

                Assert.True(
                    index1 == index3, $"Failed at {i}: {index3} <=> {index3}.");
            }
            for (nuint i = 0; i < rrrBits.UnsetBitsCount; i++)
            {
                Assert.True(
                    bits.SelectUnsetBits(i) ==
                    rrrBits.SelectUnsetBits(i),
                    $"Failed at {i}.");

                Assert.True(
                    bits.SelectUnsetBits(i) ==
                    rrrBitsFromSize.SelectUnsetBits(i),
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
            var rrrBitsBuilder = new SuccinctBitsRRRBuilder(iterations);

            var rand = new Random();
            for (uint i = 0; i < iterations; i++)
            {
                if (rand.Next() % 2 != 0)
                {
                    bitsBuilder.Set(i);
                    rrrBitsBuilder.Set(i);
                }
                else
                {
                    bitsBuilder.Unset(i);
                    rrrBitsBuilder.Unset(i);
                }
            }

            var rrrBits = new SuccinctBitsRRRBuilder(bitsBuilder).Build();
            var rrrBitsFromSize = rrrBitsBuilder.Build();

            rrrBits.Write(writer1);
            rrrBitsFromSize.Write(writer1);

            var bits = bitsBuilder.Build();

            stream1.Seek(0, SeekOrigin.Begin);
            var rrrBits2 = SuccinctBitsRRR.Read(new BinaryReader(stream1));

            Assert.Equal(rrrBits.Size, rrrBits2.Size);
            for (nuint i = 0; i < rrrBits.Size; i++)
            {
                Assert.Equal(rrrBits.GetBit(i), bits.GetBit(i));
                Assert.Equal(rrrBits.GetBit(i), rrrBits2.GetBit(i));
                Assert.Equal(rrrBitsFromSize.GetBit(i), bits.GetBit(i));
                Assert.Equal(rrrBitsFromSize.GetBit(i), rrrBits2.GetBit(i));
            }

            Assert.Equal(rrrBits, rrrBits2);
            Assert.Equal(rrrBitsFromSize, rrrBits2);
        }
    }
}
