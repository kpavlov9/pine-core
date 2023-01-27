using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;

namespace PineCore.tests.DataStructuresTests.SuccinctDataStructuresTests
{
    public class BitsTests
    {
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

                Assert.Equal(nuint.MaxValue, bitsBuilder.GetData()[0]);
                Assert.Equal(nuint.MaxValue, bitsBuilder.GetData()[1]);
                Assert.Equal((NUIntOne << 11) - 1 << 53, bitsBuilder.GetData()[2]);
            }
            else
            {// 64-Bit System:
                B11111 = (nuint)Convert.ToUInt64("11111", 2);

                for (var i = 0; i < 15; i++)
                {
                    bitsBuilder.AddBits(B11111, 5);
                }

                Assert.Equal((nuint)75, bitsBuilder.Size);
                Assert.Equal(nuint.MaxValue, bitsBuilder.GetData()[0]);
                Assert.Equal((NUIntOne << 11) - 1 << 53, bitsBuilder.GetData()[1]);
            }

            var bits = bitsBuilder.Build();
            for (nuint i = 0; i < 15; i++)
            {
                nuint value = bits.FetchBits(i * 5, 5);
                Assert.Equal(value, B11111);
            }
        }

        [Fact]
        public void set_and_get_bit()
        {
            var bitsBuilder = new BitsBuilder(128);

            bitsBuilder.SetBit(64, true);
            var bits = bitsBuilder.Build();
            Assert.True(bits.GetBit(64));

            nuint B11111 = Is32BitSystem
                ? Convert.ToUInt32("11111", 2)
                : (nuint)Convert.ToUInt64("11111", 2);

            for (uint i = 0; i < 128; i++)
            {
                bitsBuilder.SetBit(i, true);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => bitsBuilder.SetBit(128, true));

            bits = bitsBuilder.Build();

            for (nuint i = 0; i < 128; i++)
            {
                Assert.True(bits.GetBit(i));
            }

            Assert.Throws<IndexOutOfRangeException>(() => bits.GetBit(128));
        }

        [Fact]
        public void add_ones()
        {
            var bitsBuilder = new BitsBuilder(1024);

            for (var i = 0; i < 15; i++)
            {
                bitsBuilder.AddBits(true, 100);
            }

            Assert.Equal((nuint)1500, bitsBuilder.Size);

            Assert.Equal(nuint.MaxValue, bitsBuilder.GetData()[0]);

            var bits = bitsBuilder.Build();

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

            for (int i = 0; i < 15; i++)
            {
                bitsBuilder.AddBits(false, 100);
            }

            Assert.Equal((nuint)1500, bitsBuilder.Size);

            Assert.Equal(NUIntZero, bitsBuilder.GetData()[0]);

            var bits = bitsBuilder.Build();

            for (nuint i = 0; i < 1500; i++)
            {
                nuint v = bits.FetchBits(i, 1);
                Assert.Equal(NUIntZero, v);
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

            for (int i = 0; i < 15; i++)
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
            nuint[] values = new nuint[] { 1, 1, 1 };
            var bitsBuilder = new BitsBuilder(values);
            var bits = bitsBuilder.Build();

            if (Is32BitSystem)
            {// 32-Bit System:
                Assert.True(bits.GetBit(31));
                Assert.True(bits.GetBit(63));
                Assert.True(bits.GetBit(95));
                Assert.Equal(NUIntOne, bits.FetchBits(0));
                Assert.Equal(NUIntOne, bits.FetchBits(32));
                Assert.Equal(NUIntOne, bits.FetchBits(64));
            }
            else
            {// 64-Bit System:
                Assert.True(bits.GetBit(63));
                Assert.True(bits.GetBit(127));
                Assert.True(bits.GetBit(191));

                Assert.Equal(NUIntOne, bits.FetchBits(0));
                Assert.Equal(NUIntOne, bits.FetchBits(64));
                Assert.Equal(NUIntOne, bits.FetchBits(128));
            }
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
                Assert.Equal(NUIntOne, bits.FetchBits(0));
                Assert.Equal(NUIntOne, bits.FetchBits(32));
                Assert.Equal(NUIntOne, bits.FetchBits(64));
            }
            else
            {// 64-Bit System:
                Assert.True(bits.GetBit(63));
                Assert.True(bits.GetBit(127));
                Assert.True(bits.GetBit(191));

                Assert.Equal(NUIntOne, bits.FetchBits(0));
                Assert.Equal(NUIntOne, bits.FetchBits(64));
                Assert.Equal(NUIntOne, bits.FetchBits(128));
            }
        }

        [Fact]
        public void from_byte_list()
        {
            var values = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            var bitsBuilder = new BitsBuilder(values);
            var bits = bitsBuilder.Build();

            Assert.True(bits.GetBit(7));
            Assert.True(bits.GetBit(15));
            Assert.True(bits.GetBit(23));
            Assert.True(bits.GetBit(71));

            nuint value = values[0];
            for (int i = 1; i < 8; i++)
            {
                value <<= 8;
                value |= values[i];
            }

            Assert.Equal(bits.FetchBits(0), value);
        }

        [Fact]
        public void from_byte_enumerable()
        {
            var values = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 };
            IEnumerable<byte> e = values;
            var bitsBuilder = new BitsBuilder(e);
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

            Assert.Equal(bits.FetchBits(0), value);
        }
    }
}
