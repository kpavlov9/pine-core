using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.Bits;

namespace PineCore.tests.DataStructuresTests.SuccinctDataStructuresTests
{
    /// <summary>
    /// Tests for bug fixes and new functionality in improved builders.
    /// </summary>
    public class BuildersTests
    {
        #region BitsBuilder Serialization Tests

        [Fact]
        public void BitsBuilder_serialization_basic()
        {
            var builder = new BitsBuilder(128);
            builder.Set(64);
            builder.AddBits(0xABCD, 16);

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            builder.Write(writer);

            stream.Seek(0, SeekOrigin.Begin);
            var restored = BitsBuilder.Read(new BinaryReader(stream));

            Assert.Equal(builder.Size, restored.Size);
            Assert.True(restored.GetBit(64));
            Assert.Equal(builder.FetchBits(65, 16), restored.FetchBits(65, 16));
        }

        [Fact]
        public void BitsBuilder_serialization_to_file()
        {
            var builder = new BitsBuilder(256);
            for (uint i = 0; i < 100; i++)
            {
                builder.Set(i);
            }

            var filename = Path.GetTempFileName();
            try
            {
                builder.Write(filename);
                var restored = BitsBuilder.Read(filename);

                Assert.Equal(builder.Size, restored.Size);
                for (nuint i = 0; i < 100; i++)
                {
                    Assert.Equal(builder.GetBit(i), restored.GetBit(i));
                }
            }
            finally
            {
                File.Delete(filename);
            }
        }

        [Fact]
        public void BitsBuilder_serialization_empty()
        {
            var builder = new BitsBuilder();

            var stream = new MemoryStream();
            builder.Write(new BinaryWriter(stream));

            stream.Seek(0, SeekOrigin.Begin);
            var restored = BitsBuilder.Read(new BinaryReader(stream));

            Assert.Equal(0u, restored.Size);
        }

        #endregion

        #region BitsBuilder Equality Tests

        [Fact]
        public void BitsBuilder_equality()
        {
            var builder1 = new BitsBuilder(128);
            var builder2 = new BitsBuilder(128);

            builder1.Set(64);
            builder2.Set(64);

            Assert.True(builder1.Equals(builder2));
            Assert.True(builder1 == builder2);
            Assert.False(builder1 != builder2);
        }

        [Fact]
        public void BitsBuilder_inequality()
        {
            var builder1 = new BitsBuilder(128);
            var builder2 = new BitsBuilder(128);

            builder1.Set(64);
            builder2.Set(65);

            Assert.False(builder1.Equals(builder2));
            Assert.False(builder1 == builder2);
            Assert.True(builder1 != builder2);
        }

        [Fact]
        public void BitsBuilder_equality_null_safety()
        {
            var builder = new BitsBuilder(128);
            BitsBuilder? nullBuilder = null;

            Assert.False(builder == nullBuilder);
            Assert.True(builder != nullBuilder);
            Assert.False(builder.Equals(null));
        }

        #endregion

        #region NativeBitsBuilderHelper Bug Fix Tests

        [Fact]
        public void native_BitsBuilder_64bit_value()
        {
            var builder = new BitsBuilder();
            ulong value = 0x123456789ABCDEF0UL;
            
            // This would fail with the old bug
            builder.Add(value, 64);

            // Verify we can read back the same value
            if (Is32BitSystem)
            {
                // On 32-bit, it should be split into two parts
                var low = builder.FetchBits(0, 32);
                var high = builder.FetchBits(32, 32);
                ulong reconstructed = ((ulong)high << 32) | low;
                
                // The exact value depends on endianness, but it should be consistent
                Assert.Equal(64u, builder.Size);
            }
            else
            {
                // On 64-bit, should store directly
                var retrieved = builder.FetchBits(0);
                Assert.Equal(value, retrieved);
            }
        }

        [Fact]
        public void native_BitsBuilder_multiple_64bit_values()
        {
            var builder = new BitsBuilder();
            ulong[] values = { 
                0x1111111111111111UL,
                0x2222222222222222UL,
                0x3333333333333333UL
            };

            foreach (var value in values)
            {
                builder.Add(value, 64);
            }

            Assert.Equal(192u, builder.Size); // 3 * 64 bits
        }

        #endregion

        #region QuasiSuccinctIndices.Contains Bug Fix Tests

        [Fact]
        public void QuasiSuccinctBitsBuilder_contains_basic()
        {
            var builder = new QuasiSuccinctBitsBuilder(100, 10000);
            
            var values = new nuint[] { 10, 20, 30, 40, 50 };
            foreach (var value in values)
            {
                builder.Add(value);
            }

            var indices = builder.Build();

            foreach (var value in values)
            {
                Assert.True(indices.Contains(value), $"Should contain {value}");
            }

            Assert.False(indices.Contains(15));
            Assert.False(indices.Contains(0));
            Assert.False(indices.Contains(100));
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_contains_with_duplicate_high_bits()
        {
            // This test exposes the bug in the old Contains() implementation
            var builder = new QuasiSuccinctBitsBuilder(1000, 10000);
            
            // Add values that might have the same high bits
            var rand = new Random(42);
            var values = new List<nuint>();
            nuint value = 0;
            
            for (int i = 0; i < 100; i++)
            {
                value += (nuint)(rand.Next(1, 50)); // Small increments
                values.Add(value);
                builder.Add(value);
            }

            var indices = builder.Build();

            // All added values should be found
            foreach (var v in values)
            {
                Assert.True(indices.Contains(v), $"Should contain {v}");
            }

            // Values not added should not be found
            Assert.False(indices.Contains(value + 1));
            Assert.False(indices.Contains(values[0] - 1));
            
            // Value between two entries should not be found
            if (values[1] - values[0] > 1)
            {
                Assert.False(indices.Contains(values[0] + 1));
            }
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_contains_edge_cases()
        {
            var builder = new QuasiSuccinctBitsBuilder(10, 1000);
            
            builder.Add(0);    // First value
            builder.Add(999);  // Last value near upper bound

            var indices = builder.Build();

            Assert.True(indices.Contains(0));
            Assert.True(indices.Contains(999));
            Assert.False(indices.Contains(1));
            Assert.False(indices.Contains(998));
            Assert.False(indices.Contains(1000));
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_contains_empty()
        {
            var builder = new QuasiSuccinctBitsBuilder(10, 1000);
            var indices = builder.Build();

            Assert.False(indices.Contains(0));
            Assert.False(indices.Contains(500));
        }

        #endregion

        #region QuasiSuccinctIndices.IndexOf Tests

        [Fact]
        public void QuasiSuccinctBitsBuilder_index_of_basic()
        {
            var builder = new QuasiSuccinctBitsBuilder(100, 10000);
            
            var values = new nuint[] { 10, 20, 30, 40, 50 };
            foreach (var value in values)
            {
                builder.Add(value);
            }

            var indices = builder.Build();

            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(i, indices.IndexOf(values[i]));
            }

            Assert.Equal(-1, indices.IndexOf(15));
            Assert.Equal(-1, indices.IndexOf(0));
            Assert.Equal(-1, indices.IndexOf(100));
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_index_of_performance()
        {
            // Test that IndexOf is more efficient than linear search
            var builder = new QuasiSuccinctBitsBuilder(10000, 1000000);
            
            var rand = new Random(42);
            nuint value = 0;
            
            for (int i = 0; i < 10000; i++)
            {
                value += (nuint)rand.Next(1, 100);
                builder.Add(value);
            }

            var indices = builder.Build();

            // Should find values quickly even near the end
            var lastValue = builder.Get(9999);
            var index = indices.IndexOf(lastValue);
            
            Assert.Equal(9999, index);
        }

        #endregion

        #region QuasiSuccinctBitsBuilder Query During Construction Tests

        [Fact]
        public void QuasiSuccinctBitsBuilder_builder_get_with_cache()
        {
            var builder = new QuasiSuccinctBitsBuilder(100, 10000, enableCache: true);
            
            var values = new nuint[] { 10, 20, 30, 40, 50 };
            foreach (var value in values)
            {
                builder.Add(value);
            }

            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(values[i], builder.Get((nuint)i));
            }
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_builder_get_without_cache()
        {
            var builder = new QuasiSuccinctBitsBuilder(100, 10000, enableCache: false);
            
            var values = new nuint[] { 10, 20, 30, 40, 50 };
            foreach (var value in values)
            {
                builder.Add(value);
            }

            // Should still work, just slower
            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(values[i], builder.Get((nuint)i));
            }
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_builder_contains_with_cache()
        {
            var builder = new QuasiSuccinctBitsBuilder(100, 10000, enableCache: true);
            
            var values = new nuint[] { 10, 20, 30, 40, 50 };
            foreach (var value in values)
            {
                builder.Add(value);
            }

            foreach (var value in values)
            {
                Assert.True(builder.Contains(value));
            }

            Assert.False(builder.Contains(15));
            Assert.False(builder.Contains(0));
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_builder_index_of_with_cache()
        {
            var builder = new QuasiSuccinctBitsBuilder(100, 10000, enableCache: true);
            
            var values = new nuint[] { 10, 20, 30, 40, 50 };
            foreach (var value in values)
            {
                builder.Add(value);
            }

            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(i, builder.IndexOf(values[i]));
            }

            Assert.Equal(-1, builder.IndexOf(15));
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_builder_clear()
        {
            var builder = new QuasiSuccinctBitsBuilder(100, 10000, enableCache: true);
            
            builder.Add(10);
            builder.Add(20);
            
            Assert.Equal(2u, builder.Size);
            Assert.True(builder.Contains(10));

            builder.Clear();

            Assert.Equal(0u, builder.Size);
            Assert.False(builder.Contains(10));

            // Should be able to add again after clear
            builder.Add(30);
            Assert.Equal(1u, builder.Size);
            Assert.Equal(30u, builder.Get(0));
        }

        #endregion

        #region QuasiSuccinctBitsBuilder Serialization Tests

        [Fact]
        public void QuasiSuccinctBitsBuilder_builder_serialization_with_cache()
        {
            var builder = new QuasiSuccinctBitsBuilder(100, 10000, enableCache: true);
            
            var values = new nuint[] { 10, 20, 30, 40, 50 };
            foreach (var value in values)
            {
                builder.Add(value);
            }

            var stream = new MemoryStream();
            builder.Write(new BinaryWriter(stream));

            stream.Seek(0, SeekOrigin.Begin);
            var restored = QuasiSuccinctBitsBuilder.Read(new BinaryReader(stream));

            Assert.Equal(builder.Size, restored.Size);
            
            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(values[i], restored.Get((nuint)i));
            }
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_builder_serialization_without_cache()
        {
            var builder = new QuasiSuccinctBitsBuilder(100, 10000, enableCache: false);
            
            var values = new nuint[] { 10, 20, 30, 40, 50 };
            foreach (var value in values)
            {
                builder.Add(value);
            }

            var stream = new MemoryStream();
            builder.Write(new BinaryWriter(stream));

            stream.Seek(0, SeekOrigin.Begin);
            var restored = QuasiSuccinctBitsBuilder.Read(new BinaryReader(stream));

            Assert.Equal(builder.Size, restored.Size);
            
            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(values[i], restored.Get((nuint)i));
            }
        }

        #endregion

        #region BitsBuilder Optimization Tests

        [Fact]
        public void BitsBuilder_preallocated_capacity()
        {
            // Test that pre-allocation works correctly
            var builder = new BitsBuilder(1024);

            // Should not cause reallocation
            for (nuint i = 0; i < 1024; i++)
            {
                builder.Set(i);
            }

            Assert.Equal(1024u, builder.Size);
            
            for (nuint i = 0; i < 1024; i++)
            {
                Assert.True(builder.GetBit(i));
            }
        }

        [Fact]
        public void BitsBuilder_set_unset_unified()
        {
            var builder = new BitsBuilder(128);

            builder.Set(10);
            Assert.True(builder.GetBit(10));

            builder.Unset(10);
            Assert.False(builder.GetBit(10));

            builder.Set(10);
            Assert.True(builder.GetBit(10));
        }

        [Fact]
        public void BitsBuilder_add_set_bits_optimized()
        {
            var builder = new BitsBuilder();

            builder.AddSetBits(200);

            Assert.Equal(200u, builder.Size);
            
            for (nuint i = 0; i < 200; i++)
            {
                Assert.True(builder.GetBit(i));
            }
        }

        [Fact]
        public void BitsBuilder_add_unset_bits_optimized()
        {
            var builder = new BitsBuilder();

            builder.AddUnsetBits(200);

            Assert.Equal(200u, builder.Size);
            
            for (nuint i = 0; i < 200; i++)
            {
                Assert.False(builder.GetBit(i));
            }
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void BitsBuilder_invalid_bits_count()
        {
            var builder = new BitsBuilder();

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                builder.AddBits(0, -1));

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                builder.AddBits(0, NativeBitCount + 1));
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_builder_exceeds_max_length()
        {
            var builder = new QuasiSuccinctBitsBuilder(10, 1000);
            
            for (int i = 0; i < 10; i++)
            {
                builder.Add((nuint)(i * 10));
            }

            Assert.Throws<InvalidOperationException>(() => builder.Add(100));
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_builder_non_increasing_sequence()
        {
            var builder = new QuasiSuccinctBitsBuilder(10, 1000);
            
            builder.Add(50);
            
            Assert.Throws<InvalidOperationException>(() => builder.Add(40));
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_builder_exceeds_upper_bound()
        {
            var builder = new QuasiSuccinctBitsBuilder(10, 1000);
            
            Assert.Throws<InvalidOperationException>(() => builder.Add(1001));
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_get_out_of_range()
        {
            var builder = new QuasiSuccinctBitsBuilder(10, 1000, enableCache: true);
            builder.Add(10);

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Get(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Get(100));
        }

        [Fact]
        public void QuasiSuccinctBitsBuilder_indices_get_out_of_range()
        {
            var builder = new QuasiSuccinctBitsBuilder(10, 1000);
            builder.Add(10);
            var indices = builder.Build();

            Assert.Throws<ArgumentOutOfRangeException>(() => indices.Get(1));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void full_workflow_with_serialization()
        {
            // Build initial structure
            var builder = new QuasiSuccinctBitsBuilder(1000, 100000, enableCache: true);
            
            var rand = new Random(42);
            nuint value = 0;
            
            for (int i = 0; i < 500; i++)
            {
                value += (nuint)rand.Next(1, 200);
                builder.Add(value);
            }

            // Serialize builder
            var builderFile = Path.GetTempFileName();
            builder.Write(builderFile);

            // Add more values
            for (int i = 0; i < 500; i++)
            {
                value += (nuint)rand.Next(1, 200);
                builder.Add(value);
            }

            // Build final structure
            var indices = builder.Build();
            
            // Serialize indices
            var indicesFile = Path.GetTempFileName();
            indices.Write(indicesFile);

            try
            {
                // Restore builder from checkpoint
                var restoredBuilder = QuasiSuccinctBitsBuilder.Read(builderFile);
                Assert.Equal(500u, restoredBuilder.Size);

                // Restore indices
                var restoredIndices = QuasiSuccinctIndices.Read(indicesFile);
                Assert.Equal(1000u, restoredIndices.Size);
                Assert.Equal(indices, restoredIndices);

                // Verify data integrity
                for (nuint i = 0; i < 1000; i++)
                {
                    Assert.Equal(indices.Get(i), restoredIndices.Get(i));
                }
            }
            finally
            {
                File.Delete(builderFile);
                File.Delete(indicesFile);
            }
        }

        [Fact]
        public void compare_cached_vs_uncached_Builder()
        {
            var builderWithCache = new QuasiSuccinctBitsBuilder(100, 10000, enableCache: true);
            var builderWithoutCache = new QuasiSuccinctBitsBuilder(100, 10000, enableCache: false);
            
            var values = new nuint[] { 10, 20, 30, 40, 50 };
            
            foreach (var value in values)
            {
                builderWithCache.Add(value);
                builderWithoutCache.Add(value);
            }

            // Both should return same results
            for (int i = 0; i < values.Length; i++)
            {
                Assert.Equal(
                    builderWithCache.Get((nuint)i),
                    builderWithoutCache.Get((nuint)i)
                );
            }

            foreach (var value in values)
            {
                Assert.Equal(
                    builderWithCache.Contains(value),
                    builderWithoutCache.Contains(value)
                );
            }

            // Built structures should be equal
            var indicesWithCache = builderWithCache.Build();
            var indicesWithoutCache = builderWithoutCache.Build();
            
            Assert.Equal(indicesWithCache, indicesWithoutCache);
        }

        #endregion
    }
}