using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;

namespace PineCore.tests.DataStructuresTests.SuccinctDataStructuresTests
{
    public class QuasiSuccinctBitsTests
    {
        [Fact]
        public void set_and_get()
        {
            const int upperBound = 100 * 10000;
            var bitsBuilder = new QuasiSuccinctBitsBuilder(10000, upperBound);
            var rand = new Random();
            var value = (nuint)rand.Next() % 100;
            var values = new List<nuint>(10000);

            for (var i = 0; i < 10000; i++)
            {
                bitsBuilder.AddBits(value);
                values.Add(value);
                value += (nuint)rand.Next() % 100;
            }

            Assert.Throws<InvalidOperationException>(() => bitsBuilder.AddBits(value + 1));

            var bits = bitsBuilder.Build();

            for (var i = 0; i < 10000; i++)
            {
                Assert.Equal(bits.GetBit((nuint)i), values[i]);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => bits.GetBit(10000));
        }

        [Fact]
        public void read_and_write()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            const int length = 100000;
            const int upperBound = 100 * length;

            var bitsBuilder = new QuasiSuccinctBitsBuilder(length, upperBound);
            var rand = new Random();
            var value = (nuint)rand.Next() % 100;
            var values = new List<nuint>((int)length);

            for (nuint i = 0; i < length; i++)
            {
                bitsBuilder.AddBits(value);
                values.Add(value);
                value += (nuint)rand.Next() % 100;
            }

            var bits = bitsBuilder.Build();
            bits.Write(writer);

            stream.Seek(0, SeekOrigin.Begin);

            var bits2 = QuasiSuccinctBits.Read(new BinaryReader(stream));

            Assert.Equal(bits.Size, bits2.Size);
            Assert.Equal(bits.Size, bitsBuilder.Size);
            for (nuint i = 0; i < bits.Size; i++)
            {
                Assert.Equal(bits.GetBit(i), bits2.GetBit(i));
            }

            Assert.Equal(bits, bits2);
        }
    }
}
