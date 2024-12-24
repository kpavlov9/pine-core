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
                _bv0Builder.Set(i);
            }

            foreach (var i in _values)
            {
                _bv1Builder.Set(i);
                _bv0Builder.Unset(i);
            }

            _bits1 = _bv1Builder.Build();
            _bits0 = _bv0Builder.Build();
        }

        [Fact]
        public void size()
        {
            Assert.Equal(_values[^1] + 1, _bits1.Size);
            Assert.Equal((nuint)_values.Count, _bits1.SetBitsCount);
            Assert.Equal(_values[^1] + 1, _bits0.Size);
            Assert.Equal((nuint)_values.Count, _bits0.UnsetBitsCount);

            Assert.Equal(_values[^1] + 1, _bv1Builder.Size);
            Assert.Equal((nuint)_values.Count, _bv1Builder.SetBitsCount);
            Assert.Equal(_values[^1] + 1, _bv0Builder.Size);
            Assert.Equal((nuint)_values.Count, _bv0Builder.UnsetBitsCount);
        }

        [Fact]
        public void get()
        {
            foreach (nuint v in _values)
            {
                Assert.True(_bits1.GetBit(v));
                Assert.False(_bits0.GetBit(v));
                Assert.True(_bv1Builder.GetBit(v));
                Assert.False(_bv0Builder.GetBit(v));
            }
        }

        [Fact]
        public void rank_before_build()
        {
            var bits = new SuccinctBits();
            var bitsBuilder = new SuccinctBitsBuilder();

            Assert.Throws<IndexOutOfRangeException>(() => bits.RankSetBits(100));
        }

        [Fact]
        public void rank()
        {
            nuint ranksCount = 0;
            foreach (var v in _values)
            {
                Assert.Equal(ranksCount, _bits1.RankSetBits(v));
                Assert.Equal(ranksCount, _bits0.RankUnsetBits(v));

                ranksCount++;
            }
        }


        [Fact]
        public void select_bits_simple()
        {
            var bits1Builder = new BitsBuilder(87);

            for (nuint i = 0; i < 87; i++)
            {
                if(i == 0 || i == 1 || i == 29 || i == 30)
                {
                    bits1Builder.Set(i);
                }
            }

            var compressedBits1 = new SuccinctBitsBuilder(bits1Builder).Build();

            Assert.Equal((nuint)0, compressedBits1.SelectSetBits(0));
            Assert.Equal((nuint)1, compressedBits1.SelectSetBits(1));

            Assert.Equal((nuint)29, compressedBits1.SelectSetBits(2));
            Assert.Equal((nuint)30, compressedBits1.SelectSetBits(3));

            Assert.Equal(bits1Builder.Size, compressedBits1.SelectSetBits(4));
        }

        [Fact]
        public void select()
        {
            nuint positionsCount = 0;
            foreach (nuint v in _values)
            {
                Assert.Equal(v, _bits1.SelectSetBits(positionsCount));
                Assert.Equal(v, _bits0.SelectUnsetBits(positionsCount));

                positionsCount++;
            }
        }

        [Fact]
        public void select_before_build()
        {
            var bv = new SuccinctBits();

            Assert.Equal(bv.Size, bv.SelectSetBits(100));
        }

        [Fact]
        public void get_boundary()
        {
            Assert.Throws<IndexOutOfRangeException>(
                () => _bits1.GetBit(_bits1.Size));
            Assert.Throws<IndexOutOfRangeException>(
                () => _bits0.GetBit(_bits0.Size));

            Assert.Throws<IndexOutOfRangeException>(
                () => _bv1Builder.GetBit(_bv1Builder.Size));
            Assert.Throws<IndexOutOfRangeException>(
                () => _bv0Builder.GetBit(_bv0Builder.Size));
        }

        [Fact]
        public void rank_boundary()
        {
            Assert.Throws<IndexOutOfRangeException>(
                () => _bits1.RankUnsetBits(_bits0.Size + 1));
            Assert.Throws<IndexOutOfRangeException>(
                () => _bits1.RankSetBits(_bits0.Size + 1));
            Assert.Throws<IndexOutOfRangeException>(
                () => _bits1.RankUnsetBits(_bits1.Size + 1));
            Assert.Throws<IndexOutOfRangeException>(
                () => _bits1.RankSetBits(_bits1.Size + 1));
        }

        [Fact]
        public void select_boundary()
        {
            Assert.Equal(
                _bits1.Size, _bits1.SelectSetBits(_bits1.SetBitsCount));
            Assert.Equal(
                _bits1.Size, _bits1.SelectUnsetBits(_bits1.UnsetBitsCount));
            Assert.Equal(
                 _bits0.Size, _bits0.SelectSetBits(_bits0.SetBitsCount));
            Assert.Equal(
                _bits0.Size, _bits0.SelectUnsetBits(_bits0.UnsetBitsCount));
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
            Assert.Equal(_bits1.SetBitsCount, bits1Clone.SetBitsCount);
            Assert.Equal(_bits1.UnsetBitsCount, bits1Clone.UnsetBitsCount);
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
            Assert.Equal(_bits0.SetBitsCount, bv0Clone.SetBitsCount);
            Assert.Equal(_bits0.UnsetBitsCount, bv0Clone.UnsetBitsCount);

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
                if(rand.Next() % 2 != 0)
                {
                    bitsBuilder.Set(i);
                }
                else
                {
                    bitsBuilder.Unset(i);
                }
            }

            var succinctBitsBuilder = new SuccinctBitsBuilder(bitsBuilder);
            var bits = bitsBuilder.Build();

            Assert.Equal(length, succinctBitsBuilder.Size);
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
            bits.AddBytes(bytes);

            var bitsBuilder1 = new SuccinctBitsBuilder(bytes);
            var bitsBuilder2 = new SuccinctBitsBuilder(bits);

            var bits1 = bitsBuilder1.Build();
            var bits2 = bitsBuilder2.Build();

            Assert.True(bits1.Size == bits2.Size);
            Assert.True(bitsBuilder1.Size == bitsBuilder2.Size);

            for (nuint i = 0; i < count; i++)
            {
                Assert.True(bits1.GetBit(i) == bits2.GetBit(i), $"Failed at {i}.");
                Assert.True(bits1.SelectSetBits(i) == bits2.SelectSetBits(i), $"Failed at {i}.");
                Assert.True(bits1.SelectUnsetBits(i) == bits2.SelectUnsetBits(i), $"Failed at {i}.");
                Assert.True(bits1.RankSetBits(i) == bits2.RankSetBits(i), $"Failed at {i}.");
                Assert.True(bits1.RankUnsetBits(i) == bits2.RankUnsetBits(i), $"Failed at {i}.");
            }
        }


        [Fact]
        public void clear()
        {
            var bitsBuilder = new SuccinctBitsBuilder();

            Assert.True(bitsBuilder.Size == 0);

            bitsBuilder.Set(64);

            Assert.True(bitsBuilder.Size == 64 + 1);

            bitsBuilder.Clear();

            Assert.True(bitsBuilder.Size == 0);
        }

        [Fact]
        public void clear_and_build_succinct_indices_bits()
        {

            var bitsBuilder0 = new BitsBuilder(_bits0);
            var bitsBuilder1 = new BitsBuilder(_bits1);

            bitsBuilder0.Unset(1);
            bitsBuilder1.Set(1);

            var succinctIndices0 = bitsBuilder0.ClearAndBuildSuccinctIndices(_bits0);
            var succinctIndices1 = bitsBuilder1.ClearAndBuildSuccinctIndices(_bits1);

            Assert.Equal(succinctIndices0.Size, bitsBuilder0.Size);
            Assert.Equal(succinctIndices1.Size, bitsBuilder1.Size);

            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices1.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices1.SetBitsCount);
            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices0.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices0.UnsetBitsCount);

            foreach (nuint v in _values)
            {
                Assert.True(succinctIndices1.GetBit(v));
                Assert.False(succinctIndices0.GetBit(v));
            }

            nuint ranksCount = 0;
            foreach (var v in _values)
            {
                Assert.Equal(ranksCount, succinctIndices1.RankSetBits(v));
                Assert.Equal(ranksCount, succinctIndices0.RankUnsetBits(v));

                ranksCount++;
            }

            nuint positionsCount = 0;
            foreach (nuint v in _values)
            {
                Assert.Equal(v, succinctIndices1.SelectSetBits(positionsCount));
                Assert.Equal(v, succinctIndices0.SelectUnsetBits(positionsCount));

                positionsCount++;
            }
        }

        [Fact]
        public void clear_and_build_succinct_indices_succinct()
        {
            var bitsBuilder0 = new SuccinctBitsBuilder(_bits0);
            var bitsBuilder1 = new SuccinctBitsBuilder(_bits1);

            bitsBuilder0.Unset(1);
            bitsBuilder1.Set(1);

            var succinctIndices0 = bitsBuilder0.ClearAndBuildSuccinctIndices(_bits0);
            var succinctIndices1 = bitsBuilder1.ClearAndBuildSuccinctIndices(_bits1);

            Assert.Equal(succinctIndices0.Size, bitsBuilder0.Size);
            Assert.Equal(succinctIndices1.Size, bitsBuilder1.Size);

            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices1.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices1.SetBitsCount);
            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices0.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices0.UnsetBitsCount);

            foreach (nuint v in _values)
            {
                Assert.True(succinctIndices1.GetBit(v));
                Assert.False(succinctIndices0.GetBit(v));
            }

            nuint ranksCount = 0;
            foreach (var v in _values)
            {
                Assert.Equal(ranksCount, succinctIndices1.RankSetBits(v));
                Assert.Equal(ranksCount, succinctIndices0.RankUnsetBits(v));

                ranksCount++;
            }

            nuint positionsCount = 0;
            foreach (nuint v in _values)
            {
                Assert.Equal(v, succinctIndices1.SelectSetBits(positionsCount));
                Assert.Equal(v, succinctIndices0.SelectUnsetBits(positionsCount));

                positionsCount++;
            }
        }

        [Fact]
        public void clear_and_build_succinct_indices_succinct_compressed()
        {
            var bitsBuilder0 = new SuccinctCompressedBitsBuilder(_bits0);
            var bitsBuilder1 = new SuccinctCompressedBitsBuilder(_bits1);

            bitsBuilder0.Unset(1);
            bitsBuilder1.Set(1);

            var succinctIndices0 = bitsBuilder0.ClearAndBuildSuccinctIndices(_bits0);
            var succinctIndices1 = bitsBuilder1.ClearAndBuildSuccinctIndices(_bits1);

            Assert.Equal(succinctIndices0.Size, bitsBuilder0.Size);
            Assert.Equal(succinctIndices1.Size, bitsBuilder1.Size);

            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices1.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices1.SetBitsCount);
            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices0.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices0.UnsetBitsCount);

            foreach (nuint v in _values)
            {
                Assert.True(succinctIndices1.GetBit(v));
                Assert.False(succinctIndices0.GetBit(v));
            }

            nuint ranksCount = 0;
            foreach (var v in _values)
            {
                Assert.Equal(ranksCount, succinctIndices1.RankSetBits(v));
                Assert.Equal(ranksCount, succinctIndices0.RankUnsetBits(v));

                ranksCount++;
            }

            nuint positionsCount = 0;
            foreach (nuint v in _values)
            {
                Assert.Equal(v, succinctIndices1.SelectSetBits(positionsCount));
                Assert.Equal(v, succinctIndices0.SelectUnsetBits(positionsCount));

                positionsCount++;
            }
        }

        [Fact]
        public void build_succinct_indices_succinct()
        {
            var succinctIndices0 = _bv0Builder.BuildSuccinctBits();
            var succinctIndices1 = _bv1Builder.BuildSuccinctBits();

            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices1.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices1.SetBitsCount);
            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices0.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices0.UnsetBitsCount);

            foreach (nuint v in _values)
            {
                Assert.True(succinctIndices1.GetBit(v));
                Assert.False(succinctIndices0.GetBit(v));
            }

            nuint ranksCount = 0;
            foreach (var v in _values)
            {
                Assert.Equal(ranksCount, succinctIndices1.RankSetBits(v));
                Assert.Equal(ranksCount, succinctIndices0.RankUnsetBits(v));

                ranksCount++;
            }

            nuint positionsCount = 0;
            foreach (nuint v in _values)
            {
                Assert.Equal(v, succinctIndices1.SelectSetBits(positionsCount));
                Assert.Equal(v, succinctIndices0.SelectUnsetBits(positionsCount));

                positionsCount++;
            }
        }


        [Fact]
        public void build_succinct_indices_bits()
        {
            var bitsBuilder0 = new BitsBuilder(_bits0);
            var bitsBuilder1 = new BitsBuilder(_bits1);

            var succinctIndices0 = bitsBuilder0.BuildSuccinctBits();
            var succinctIndices1 = bitsBuilder1.BuildSuccinctBits();

            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices1.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices1.SetBitsCount);
            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices0.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices0.UnsetBitsCount);

            foreach (nuint v in _values)
            {
                Assert.True(succinctIndices1.GetBit(v));
                Assert.False(succinctIndices0.GetBit(v));
            }

            nuint ranksCount = 0;
            foreach (var v in _values)
            {
                Assert.Equal(ranksCount, succinctIndices1.RankSetBits(v));
                Assert.Equal(ranksCount, succinctIndices0.RankUnsetBits(v));

                ranksCount++;
            }

            nuint positionsCount = 0;
            foreach (nuint v in _values)
            {
                Assert.Equal(v, succinctIndices1.SelectSetBits(positionsCount));
                Assert.Equal(v, succinctIndices0.SelectUnsetBits(positionsCount));

                positionsCount++;
            }
        }

        [Fact]
        public void build_succinct_indices_succinct_compressed()
        {
            var bitsBuilder0 = new SuccinctCompressedBitsBuilder(_bits0);
            var bitsBuilder1 = new SuccinctCompressedBitsBuilder(_bits1);

            var succinctIndices0 = bitsBuilder0.BuildSuccinctBits();
            var succinctIndices1 = bitsBuilder1.BuildSuccinctBits();

            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices1.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices1.SetBitsCount);
            Assert.Equal(_values[_values.Count - 1] + 1, succinctIndices0.Size);
            Assert.Equal((nuint)_values.Count, succinctIndices0.UnsetBitsCount);

            foreach (nuint v in _values)
            {
                Assert.True(succinctIndices1.GetBit(v));
                Assert.False(succinctIndices0.GetBit(v));
            }

            nuint ranksCount = 0;
            foreach (var v in _values)
            {
                Assert.Equal(ranksCount, succinctIndices1.RankSetBits(v));
                Assert.Equal(ranksCount, succinctIndices0.RankUnsetBits(v));

                ranksCount++;
            }

            nuint positionsCount = 0;
            foreach (nuint v in _values)
            {
                Assert.Equal(v, succinctIndices1.SelectSetBits(positionsCount));
                Assert.Equal(v, succinctIndices0.SelectUnsetBits(positionsCount));

                positionsCount++;
            }
        }
    }
}
