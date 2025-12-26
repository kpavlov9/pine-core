

using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.Bits;

namespace PineCore.tests.DataStructuresTests.SuccinctDataStructuresTests
{
    public class QuasiSuccinctBitsTests
    {
        [Fact]
        public void Set_and_Get()
        {
            const int upperBound = 100 * 10000;
            var bitsBuilder = new QuasiSuccinctBitsBuilder(10000, upperBound);
            var rand = new Random();
            var value = (nuint)rand.Next() % 100;
            var values = new List<nuint>(10000);

            for (var i = 0; i < 10000; i++)
            {
                bitsBuilder.Add(value);
                values.Add(value);
                value += (nuint)rand.Next() % 100;
            }

            Assert.Throws<InvalidOperationException>(() => bitsBuilder.Add(value + 1));

            var bits = bitsBuilder.Build();

            for (var i = 0; i < 10000; i++)
            {
                Assert.Equal(bits.Get((nuint)i), values[i]);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => bits.Get(10000));
        }

        [Fact]
        public void Read_and_Write()
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
                bitsBuilder.Add(value);
                values.Add(value);
                value += (nuint)rand.Next() % 100;
            }

            var bits = bitsBuilder.Build();
            bits.Write(writer);

            stream.Seek(0, SeekOrigin.Begin);

            var bits2 = QuasiSuccinctIndices.Read(new BinaryReader(stream));

            Assert.Equal(bits.Size, bits2.Size);
            Assert.Equal(bits.Size, bitsBuilder.Size);
            for (nuint i = 0; i < bits.Size; i++)
            {
                Assert.Equal(bits.Get(i), bits2.Get(i));
            }

            Assert.Equal(bits, bits2);
        }
    }
}
