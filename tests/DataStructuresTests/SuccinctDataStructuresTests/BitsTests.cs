using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices;

namespace PineCore.tests.DataStructuresTests.SuccinctDataStructuresTests
{
    public class BitsTests
    {
        [Fact]
        public void clear()
        {
            var bitsBuilder = new BitsBuilder();

            Assert.True(bitsBuilder.Size == 0);

            bitsBuilder.Set(64);

            Assert.True(bitsBuilder.Size == 64 + 1);

            bitsBuilder.Clear();

            Assert.True(bitsBuilder.Size == 0);
        }

        [Fact]
        public void clear_and_build_bit_indices_bits()
        {
            var bitsBuilder = new BitsBuilder(128);
            bitsBuilder.Set(64);

            var bits = bitsBuilder.Build();

            bitsBuilder.Set(65);

            var bitIndices = bitsBuilder.ClearAndBuildBitIndices(bits);

            Assert.True(bitIndices.GetBit(64));
            Assert.Equal(bitIndices.Size, bitsBuilder.Size);

            for (uint i = 0; i < 128; i++)
            {
                bitsBuilder.Set(i);
            }

            bitIndices = bitsBuilder.BuildBitIndices();

            Assert.Equal(bitIndices.Size, bitsBuilder.Size);

            for (nuint i = 0; i < 128; i++)
            {
                Assert.True(bitIndices.GetBit(i));
            }
        }


        [Fact]
        public void clear_and_build_bit_indices_succinct()
        {
            var bitsBuilder = new SuccinctBitsBuilder(128);
            bitsBuilder.Set(64);

            var bits = bitsBuilder.Build();

            bitsBuilder.Set(65);

            var bitIndices = bitsBuilder.ClearAndBuildBitIndices(bits);

            Assert.True(bitIndices.GetBit(64));
            Assert.Equal(bitIndices.Size, bitsBuilder.Size);

            for (uint i = 0; i < 128; i++)
            {
                bitsBuilder.Set(i);
            }

            bitIndices = bitsBuilder.BuildBitIndices();

            Assert.Equal(bitIndices.Size, bitsBuilder.Size);

            for (nuint i = 0; i < 128; i++)
            {
                Assert.True(bitIndices.GetBit(i));
            }
        }

        [Fact]
        public void clear_and_build_bit_indices_succinct_compressed()
        {
            var compressedBitsBuilder = new SuccinctCompressedBitsBuilder(128);
            compressedBitsBuilder.Set(64);

            var bitsBuilder = new BitsBuilder(128);
            bitsBuilder.Set(64);

            var bits = bitsBuilder.Build();

            bitsBuilder.Set(65);

            var bitIndices = compressedBitsBuilder.ClearAndBuildBitIndices(bits);

            Assert.True(bitIndices.GetBit(64));
            Assert.Equal(bitIndices.Size, compressedBitsBuilder.Size);

            for (uint i = 0; i < 128; i++)
            {
                bitsBuilder.Set(i);
            }

            bitIndices = bitsBuilder.BuildBitIndices();

            Assert.Equal(bitIndices.Size, bitsBuilder.Size);

            for (nuint i = 0; i < 128; i++)
            {
                Assert.True(bitIndices.GetBit(i));
            }
        }

        [Fact]
        public void build_bit_indices_bits()
        {
            var bitsBuilder = new BitsBuilder(128);
            bitsBuilder.Set(64);

            var bitIndices = bitsBuilder.BuildBitIndices();

            Assert.True(bitIndices.GetBit(64));

            for (uint i = 0; i < 128; i++)
            {
                bitsBuilder.Set(i);
            }

            bitIndices = bitsBuilder.BuildBitIndices();

            Assert.Equal(bitIndices.Size, bitsBuilder.Size);

            for (nuint i = 0; i < 128; i++)
            {
                Assert.True(bitIndices.GetBit(i));
            }
        }


        [Fact]
        public void build_bit_indices_succinct()
        {
            var bitsBuilder = new SuccinctBitsBuilder(128);
            bitsBuilder.Set(64);

            var bitIndices = bitsBuilder.BuildBitIndices();

            Assert.True(bitIndices.GetBit(64));

            for (uint i = 0; i < 128; i++)
            {
                bitsBuilder.Set(i);
            }

            bitIndices = bitsBuilder.BuildBitIndices();

            Assert.Equal(bitIndices.Size, bitsBuilder.Size);

            for (nuint i = 0; i < 128; i++)
            {
                Assert.True(bitIndices.GetBit(i));
            }
        }

        [Fact]
        public void build_bit_indices_succinct_compressed()
        {
            var bitsBuilder = new SuccinctCompressedBitsBuilder(128);
            bitsBuilder.Set(64);

            var bitIndices = bitsBuilder.BuildBitIndices();

            Assert.True(bitIndices.GetBit(64));

            for (uint i = 0; i < 128; i++)
            {
                bitsBuilder.Set(i);
            }

            bitIndices = bitsBuilder.BuildBitIndices();

            Assert.Equal(bitIndices.Size, bitsBuilder.Size);

            for (nuint i = 0; i < 128; i++)
            {
                Assert.True(bitIndices.GetBit(i));
            }
        }

        [Fact]
        public void add_and_fetch_bits()
        {
            var bitsBuilder = new BitsBuilder(128);
            nuint B11111;

            if (Is32BitSystem)
            {// 32-Bit System:
                B11111 = Convert.ToUInt32("11111", 2);

                for (var i = 0; i < 15; i++)
                {
                    bitsBuilder.AddBits(B11111, 5);
                }
                var bitsBuilderData = bitsBuilder.Data.ToArray();

                Assert.Equal(nuint.MaxValue, bitsBuilderData[0]);
                Assert.Equal(nuint.MaxValue, bitsBuilderData[1]);
                Assert.Equal((NUIntOne << 11) - 1 << 53, bitsBuilderData[2]);
            }
            else
            {// 64-Bit System:
                B11111 = (nuint)Convert.ToUInt64("11111", 2);

                for (var i = 0; i < 15; i++)
                {
                    bitsBuilder.AddBits(B11111, 5);
                }

                var bitsBuilderData = bitsBuilder.Data.ToArray();

                Assert.Equal((nuint)75, bitsBuilder.Size);
                Assert.Equal(nuint.MaxValue, bitsBuilderData[0]);
                Assert.Equal((NUIntOne << 11) - 1 << 53, bitsBuilderData[1]);
            }

            var bits = bitsBuilder.Build();
            Assert.Equal(bits.Size, bitsBuilder.Size);

            for (nuint i = 0; i < 15; i++)
            {
                nuint value = bits.FetchBits(i * 5, 5);
                nuint builderValue = bitsBuilder.FetchBits(i * 5, 5);
                Assert.Equal(value, B11111);
                Assert.Equal(builderValue, value);
            }
        }

        [Fact]
        public void set_and_get_bit()
        {
            var bitsBuilder = new BitsBuilder(128);

            bitsBuilder.Set(64);
            var bits = bitsBuilder.Build();
            Assert.True(bits.GetBit(64));

            for (uint i = 0; i < 128; i++)
            {
                bitsBuilder.Set(i);
                Assert.True(bitsBuilder.GetBit(i) == true);
            }

            bits = bitsBuilder.Build();

            Assert.Equal(bits.Size, bitsBuilder.Size);

            for (nuint i = 0; i < 128; i++)
            {
                Assert.True(bits.GetBit(i));
                Assert.True(bitsBuilder.GetBit(i));
            }

            Assert.Throws<IndexOutOfRangeException>(() => bits.GetBit(128));
        }

        [Fact]
        public void add_ones()
        {
            var bitsBuilder = new BitsBuilder(1024);

            for (var i = 0; i < 15; i++)
            {
                bitsBuilder.AddSetBits(100);
            }

            Assert.Equal((nuint)1500, bitsBuilder.Size);

            var bitsBuilderData = bitsBuilder.Data.ToArray();

            Assert.Equal(nuint.MaxValue, bitsBuilderData[0]);

            var bits = bitsBuilder.Build();

            Assert.Equal(bits.Size, bitsBuilder.Size);

            for (nuint i = 0; i < 1500; i++)
            {
                nuint v = bits.FetchBits(i, 1);
                Assert.Equal(NUIntOne, v);
            }
        }

        [Fact]
        public void add_zeros()
        {
            var bitsBuilder = new BitsBuilder(1024);

            for (var i = 0; i < 15; i++)
            {
                bitsBuilder.AddUnsetBits(100);
            }

            Assert.Equal((nuint)1500, bitsBuilder.Size);

            var bitsBuilderData = bitsBuilder.Data.ToArray();

            Assert.Equal(NUIntZero, bitsBuilderData[0]);

            var bits = bitsBuilder.Build();

            Assert.Equal(bits.Size, bitsBuilder.Size);

            for (nuint i = 0; i < 1500; i++)
            {
                nuint v = bits.FetchBits(i, 1);
                nuint builderV = bitsBuilder.FetchBits(i, 1);

                Assert.Equal(NUIntZero, v);
                Assert.Equal(NUIntZero, builderV);
            }
        }

        [Fact]
        public void read_and_write()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            BitsBuilder bitsBuilder;
            nuint B11111;

            if (Is32BitSystem)
            {// 32-Bit System:
                bitsBuilder = new BitsBuilder(96);

                B11111 = Convert.ToUInt32("11111", 2);

            }
            else
            {// 64-Bit System:
                bitsBuilder = new BitsBuilder(128);
                B11111 = (nuint)Convert.ToUInt64("11111", 2);
            }

            for (var i = 0; i < 15; i++)
            {
                bitsBuilder.AddBits(B11111, 5);
            }

            var bits = bitsBuilder.Build();
            bits.Write(writer);

            stream.Seek(0, SeekOrigin.Begin);
            var bitsClone = Bits.Read(new BinaryReader(stream));

            Assert.Equal(bits, bitsClone);
        }

        [Fact]
        public void from_ul_list()
        {
            var values = new nuint[] { 1, 1, 1 };
            var bitsBuilder = new BitsBuilder(values);
            var bits = bitsBuilder.Build();

            if (Is32BitSystem)
            {// 32-Bit System:
                Assert.True(bits.GetBit(31));
                Assert.True(bits.GetBit(63));
                Assert.True(bits.GetBit(95));

                Assert.True(bitsBuilder.GetBit(31));
                Assert.True(bitsBuilder.GetBit(63));
                Assert.True(bitsBuilder.GetBit(95));

                Assert.Equal(NUIntOne, bits.FetchBits(0));
                Assert.Equal(NUIntOne, bits.FetchBits(32));
                Assert.Equal(NUIntOne, bits.FetchBits(64));
            }
            else
            {// 64-Bit System:
                Assert.True(bits.GetBit(63));
                Assert.True(bits.GetBit(127));
                Assert.True(bits.GetBit(191));

                Assert.True(bitsBuilder.GetBit(63));
                Assert.True(bitsBuilder.GetBit(127));
                Assert.True(bitsBuilder.GetBit(191));

                Assert.Equal(NUIntOne, bits.FetchBits(0));
                Assert.Equal(NUIntOne, bits.FetchBits(64));
                Assert.Equal(NUIntOne, bits.FetchBits(128));
            }

            Assert.Equal((nuint)values.Count() * NativeBitCount, bitsBuilder.Size);
            Assert.Equal(bits.Size, bitsBuilder.Size);
        }

        [Fact]
        public void from_ul_enumerable()
        {
            IEnumerable<nuint> values = new nuint[] { 1, 1, 1 };
            var bitsBuilder = new BitsBuilder(values);
            var bits = bitsBuilder.Build();

            if (Is32BitSystem)
            {// 32-Bit System:
                Assert.True(bits.GetBit(31));
                Assert.True(bits.GetBit(63));
                Assert.True(bits.GetBit(95));

                Assert.True(bitsBuilder.GetBit(31));
                Assert.True(bitsBuilder.GetBit(63));
                Assert.True(bitsBuilder.GetBit(95));

                Assert.Equal(NUIntOne, bits.FetchBits(0));
                Assert.Equal(NUIntOne, bits.FetchBits(32));
                Assert.Equal(NUIntOne, bits.FetchBits(64));

                Assert.Equal(NUIntOne, bitsBuilder.FetchBits(0));
                Assert.Equal(NUIntOne, bitsBuilder.FetchBits(32));
                Assert.Equal(NUIntOne, bitsBuilder.FetchBits(64));
            }
            else
            {// 64-Bit System:
                Assert.True(bits.GetBit(63));
                Assert.True(bits.GetBit(127));
                Assert.True(bits.GetBit(191));

                Assert.True(bitsBuilder.GetBit(63));
                Assert.True(bitsBuilder.GetBit(127));
                Assert.True(bitsBuilder.GetBit(191));

                Assert.Equal(NUIntOne, bits.FetchBits(0));
                Assert.Equal(NUIntOne, bits.FetchBits(64));
                Assert.Equal(NUIntOne, bits.FetchBits(128));

                Assert.Equal(NUIntOne, bitsBuilder.FetchBits(0));
                Assert.Equal(NUIntOne, bitsBuilder.FetchBits(64));
                Assert.Equal(NUIntOne, bitsBuilder.FetchBits(128));
            }


            Assert.Equal((nuint)values.Count() * NativeBitCount, bitsBuilder.Size);
            Assert.Equal(bits.Size, bitsBuilder.Size);
        }

        [Fact]
        public void from_byte_list()
        {
            var values = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            var bitsBuilder = new BitsBuilder(values);


            Assert.True(bitsBuilder.GetBit(7));
            Assert.True(bitsBuilder.GetBit(15));
            Assert.True(bitsBuilder.GetBit(23));
            Assert.True(bitsBuilder.GetBit(71));

            var bits = bitsBuilder.Build();

            Assert.True(bits.GetBit(7));
            Assert.True(bits.GetBit(15));
            Assert.True(bits.GetBit(23));
            Assert.True(bits.GetBit(71));

            nuint value = values[0];
            for (var i = 1; i < 8; i++)
            {
                value <<= 8;
                value |= values[i];
            }

            Assert.Equal((nuint)values.Length * BitCountInByte, bitsBuilder.Size);
            Assert.Equal(bits.Size, bitsBuilder.Size);

            Assert.Equal(bits.FetchBits(0), value);
            Assert.Equal(bitsBuilder.FetchBits(0), value);
        }

        [Fact]
        public void from_byte_enumerable()
        {
            var values = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 };
            IEnumerable<byte> enumerableValues = values;
            var bitsBuilder = new BitsBuilder(enumerableValues);

            Assert.True(bitsBuilder.GetBit(7));
            Assert.True(bitsBuilder.GetBit(15));
            Assert.True(bitsBuilder.GetBit(23));

            var bits = bitsBuilder.Build();

            Assert.True(bits.GetBit(7));
            Assert.True(bits.GetBit(15));
            Assert.True(bits.GetBit(23));

            nuint value = values[0];

            for (var i = 1; i < 8; i++)
            {
                value <<= 8;
                value |= values[i];
            }

            Assert.Equal((nuint)values.Length * BitCountInByte, bitsBuilder.Size);
            Assert.Equal(bits.Size, bitsBuilder.Size);

            Assert.Equal(bits.FetchBits(0), value);
            Assert.Equal(bitsBuilder.FetchBits(0), value);
        }
    }
}
