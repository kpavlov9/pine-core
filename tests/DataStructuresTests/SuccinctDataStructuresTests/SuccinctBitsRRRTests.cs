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
                bool b = rand.Next() % 2 != 0;
                bitsBuilder.SetBit(i, b);
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
        public void rank()
        {
            var rand = new Random();
            nuint bitsLength = 10000 + (nuint)rand.Next() % 10000;
            var bits = new BitsBuilder(bitsLength);
            for (uint i = 0; i < bitsLength; i++)
            {
                bool b = rand.Next() % 2 != 0;
                bits.SetBit(i, b);
            }

            var rrrBits = new SuccinctBitsRRRBuilder(bits).Build();

            var rrrBitsDiplicate = new SuccinctBitsBuilder(bits).Build();
            Assert.Equal(rrrBitsDiplicate.Size, rrrBits.Size);
            for (nuint i = 0; i < bitsLength; i++)
            {
                Assert.True(rrrBitsDiplicate.Rank(i, true) == rrrBits.Rank(i, true), $"Failed at {i}.");
                Assert.True(rrrBitsDiplicate.Rank(i, false) == rrrBits.Rank(i, false), $"Failed at {i}.");
            }
        }

        [Fact]
        public void select()
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

            var bits1 = new BitsBuilder(values.Max());
            var bits0 = new BitsBuilder(values.Max());

            for (uint i = 0; i <= values[values.Count - 1]; i++)
            {
                bits0.SetBit(i, true);
            }

            foreach (uint i in values)
            {
                bits1.SetBit(i, true);
                bits0.SetBit(i, false);
            }

            var rrrBits1 = new SuccinctBitsRRRBuilder(bits1).Build();
            var rrrBits0 = new SuccinctBitsRRRBuilder(bits0).Build();
            nuint counter = 0;
            foreach (nuint v in values)
            {
                Assert.Equal(v, rrrBits1.Select(counter, true));
                Assert.Equal(v, rrrBits0.Select(counter, false));
                counter++;
            }


            values = new List<nuint>();
            var rand = new Random();
            nuint length = 10000 + (nuint)rand.Next() % 10000;
            var bits2 = new BitsBuilder(length);
            for (uint i = 0; i < length; i++)
            {
                bool b = rand.Next() % 2 != 0;
                if (b)
                {
                    values.Add(i);
                }
                bits2.SetBit(i, b);
            }
            var rrr2 = new SuccinctBitsRRRBuilder(bits2).Build();
            counter = 0;
            foreach (nuint v in values)
            {

                Assert.Equal(v, rrr2.Select(counter, true));
                counter++;
            }

            var bits = new BitsBuilder(length);
            for (uint i = 0; i < length; i++)
            {
                bool b = rand.Next() % 2 != 0;
                bits.SetBit(i, b);
            }

            var rrrBits = new SuccinctBitsRRRBuilder(bits).Build();
            var rrrBitsDuplicate = new SuccinctBitsBuilder(bits).Build();
            Assert.Equal(rrrBitsDuplicate.GetBitsCount(true), rrrBits.GetBitsCount(true));
            Assert.Equal(rrrBitsDuplicate.GetBitsCount(false), rrrBits.GetBitsCount(false));

            for (nuint i = 0; i < rrrBits.GetBitsCount(true); i++)
            {
                nuint index1 = rrrBitsDuplicate.Select(i, true);
                nuint index2 = rrrBits.Select(i, true);
                Assert.True(index1 == index2, $"Failed at {i}: {index1} <=> {index2}.");
            }
            for (nuint i = 0; i < rrrBits.GetBitsCount(false); i++)
            {
                Assert.True(
                    rrrBitsDuplicate.Select(i, false) == rrrBits.Select(i, false),
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
            var rand = new Random();
            for (uint i = 0; i < iterations; i++)
            {
                bool b = rand.Next() % 2 != 0;
                bitsBuilder.SetBit(i, b);
            }

            var rrrBits = new SuccinctBitsRRRBuilder(bitsBuilder).Build();
            rrrBits.Write(writer1);

            var bits = bitsBuilder.Build();

            stream1.Seek(0, SeekOrigin.Begin);
            var rrrBits2 = SuccinctBitsRRR.Read(new BinaryReader(stream1));

            Assert.Equal(rrrBits.Size, rrrBits2.Size);
            for (nuint i = 0; i < rrrBits.Size; i++)
            {
                Assert.Equal(rrrBits.GetBit(i), bits.GetBit(i));
                Assert.Equal(rrrBits.GetBit(i), rrrBits2.GetBit(i));
            }

            Assert.Equal(rrrBits, rrrBits2);
        }
    }
}
