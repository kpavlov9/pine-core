using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices;

namespace PineCore.tests.DataStructuresTests.SuccinctDataStructuresTests
{
    public class SuccinctBitsTests
    {
        readonly List<nuint> _values;
        readonly SuccinctBits _bits1;
        readonly SuccinctBits _bits0;
        readonly SuccinctBitsBuilder _bv1Builder;
        readonly SuccinctBitsBuilder _bv0Builder;
        public SuccinctBitsTests()
        {
            _values = new List<nuint>
            {
                0,
                511,
                512,
                1000,
                2000,
                3000
            };

            _bv1Builder = new SuccinctBitsBuilder();
            _bv0Builder = new SuccinctBitsBuilder();
            for (nuint i = 0; i <= _values[_values.Count - 1]; i++)
            {
                _bv0Builder.Set(i, true);
            }

            foreach (var i in _values)
            {
                _bv1Builder.Set(i, true);
                _bv0Builder.Set(i, false);
            }

            _bits1 = _bv1Builder.Build();
            _bits0 = _bv0Builder.Build();
        }

        [Fact]
        public void size()
        {
            Assert.Equal(_values[_values.Count - 1] + 1, _bits1.Size);
            Assert.Equal((nuint)_values.Count, _bits1.GetBitsCount(true));
            Assert.Equal(_values[_values.Count - 1] + 1, _bits0.Size);
            Assert.Equal((nuint)_values.Count, _bits0.GetBitsCount(false));
        }

        [Fact]
        public void get()
        {
            foreach (nuint v in _values)
            {
                Assert.True(_bits1.GetBit(v));
                Assert.False(_bits0.GetBit(v));
            }
        }

        [Fact]
        public void rank_before_build()
        {
            var bits = new SuccinctBits();
            Assert.Throws<IndexOutOfRangeException>(() => bits.Rank(100, true));
        }

        [Fact]
        public void rank()
        {
            nuint ranksCount = 0;
            foreach (var v in _values)
            {
                Assert.Equal(ranksCount, _bits1.Rank(v, true));
                Assert.Equal(ranksCount, _bits0.Rank(v, false));
                ranksCount++;
            }
        }

        [Fact]
        public void select()
        {
            nuint positionsCount = 0;
            foreach (nuint v in _values)
            {
                Assert.Equal(v, _bits1.Select(positionsCount, true));
                Assert.Equal(v, _bits0.Select(positionsCount, false));
                positionsCount++;
            }
        }

        [Fact]
        public void selectBefore_build()
        {
            var bv = new SuccinctBits();
            Assert.Throws<ArgumentOutOfRangeException>(() => bv.Select(100, true));
        }

        [Fact]
        public void get_boundary()
        {
            Assert.Throws<IndexOutOfRangeException>(() => _bits1.GetBit(_bits1.Size));
            Assert.Throws<IndexOutOfRangeException>(() => _bits0.GetBit(_bits0.Size));
        }

        [Fact]
        public void rank_boundary()
        {
            Assert.Throws<IndexOutOfRangeException>(() => _bits1.Rank(_bits0.Size + 1, false));
            Assert.Throws<IndexOutOfRangeException>(() => _bits1.Rank(_bits0.Size + 1, true));
            Assert.Throws<IndexOutOfRangeException>(() => _bits1.Rank(_bits1.Size + 1, false));
            Assert.Throws<IndexOutOfRangeException>(() => _bits1.Rank(_bits1.Size + 1, true));
        }

        [Fact]
        public void select_boundary()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _bits1.Select(_bits1.GetBitsCount(true), true));
            Assert.Throws<ArgumentOutOfRangeException>(() => _bits1.Select(_bits1.GetBitsCount(false), false));
            Assert.Throws<ArgumentOutOfRangeException>(() => _bits0.Select(_bits0.GetBitsCount(true), true));
            Assert.Throws<ArgumentOutOfRangeException>(() => _bits0.Select(_bits0.GetBitsCount(false), false));
        }

        [Fact]
        public void read_and_write()
        {
            var stream1 = new MemoryStream();
            var writer1 = new BinaryWriter(stream1);
            _bits1.Write(writer1);

            stream1.Seek(0, SeekOrigin.Begin);
            var bits1Clone = SuccinctBits.Read(new BinaryReader(stream1));

            Assert.Equal(_bits1.Size, bits1Clone.Size);
            Assert.Equal(_bits1.GetBitsCount(true), bits1Clone.GetBitsCount(true));
            Assert.Equal(_bits1.GetBitsCount(false), bits1Clone.GetBitsCount(false));
            for (nuint i = 0; i < _bits1.Size; i++)
            {
                Assert.Equal(_bits1.GetBit(i), bits1Clone.GetBit(i));
            }

            var stream0 = new MemoryStream();
            var writer0 = new BinaryWriter(stream0);
            _bits0.Write(writer0);

            stream0.Seek(0, SeekOrigin.Begin);
            var bv0Clone = SuccinctBits.Read(new BinaryReader(stream0));

            Assert.Equal(_bits0.Size, bv0Clone.Size);
            Assert.Equal(_bits0.GetBitsCount(true), bv0Clone.GetBitsCount(true));
            Assert.Equal(_bits0.GetBitsCount(false), bv0Clone.GetBitsCount(false));

            for (nuint i = 0; i < _bits0.Size; i++)
            {
                Assert.Equal(_bits0.GetBit(i), bv0Clone.GetBit(i));
            }
        }

        [Fact]
        public void from_bits()
        {
            const nuint length = 10000;
            var bitsBuilder = new BitsBuilder(length);
            var rand = new Random();
            for (uint i = 0; i < length; i++)
            {
                bool b = rand.Next() % 2 != 0;
                bitsBuilder.SetBit(i, b);
            }

            var succinctBitsBuilder = new SuccinctBitsBuilder(bitsBuilder);
            var bits = bitsBuilder.Build();

            Assert.Equal(bitsBuilder.Size, succinctBitsBuilder.Size);
            for (nuint i = 0; i < length; i++)
            {
                Assert.True(bits.GetBit(i) == succinctBitsBuilder.GetBit(i), $"Failed at {i}.");
            }

        }
        [Fact]
        public void from_bytes()
        {
            const uint count = 10000;
            byte[] bytes = new byte[count];

            var rand = new Random();
            rand.NextBytes(bytes);

            var bits = new BitsBuilder(count);
            bits.AddBits(bytes);

            var bits1 = new SuccinctBitsBuilder(bytes).Build();
            var bits2 = new SuccinctBitsBuilder(bits).Build();

            Assert.True(bits1.Size == bits2.Size);
            for (nuint i = 0; i < count; i++)
            {
                Assert.True(bits1.GetBit(i) == bits2.GetBit(i), $"Failed at {i}.");
                Assert.True(bits1.Select(i, true) == bits2.Select(i, true), $"Failed at {i}.");
                Assert.True(bits1.Select(i, false) == bits2.Select(i, false), $"Failed at {i}.");
                Assert.True(bits1.Rank(i, true) == bits2.Rank(i, true), $"Failed at {i}.");
                Assert.True(bits1.Rank(i, false) == bits2.Rank(i, false), $"Failed at {i}.");
            }
        }
    }
}
