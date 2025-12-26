using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.Bits;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctBits;

namespace PineCore.tests.DataStructuresTests.SuccinctDataStructuresTests;

/// <summary>
/// Diagnostic tests to understand the IndexOutOfRangeException
/// </summary>
public class RankDiagnosticTests
{
    [Fact]
    public void diagnose_exact_failing_scenario()
    {
        // Reproduce EXACT scenario from rank_matches_succinct_bits
        const nuint size = 5000;
        var bitsBuilder = new BitsBuilder(size);
        var rand = new Random(456); // Same seed
        
        int setBitCount = 0;
        for (nuint i = 0; i < size; i++)
        {
            if (rand.Next() % 2 != 0)
            {
                bitsBuilder.Set(i);
                setBitCount++;
            }
        }

        // Log what we have
        Console.WriteLine($"Initial BitsBuilder size: {bitsBuilder.Size}");
        Console.WriteLine($"Set bits count: {setBitCount}");
        Console.WriteLine($"Expected to query positions: 0, 250, 500, ..., 5000");

        // Create the builder with rank
        var compressedBuilderWithRank = new SuccinctCompressedBitsBuilder(bitsBuilder);
        Console.WriteLine($"SuccinctCompressedBitsBuilder size: {compressedBuilderWithRank.Size}");

        // Try queries that the test does
        var testPositions = new List<nuint>();
        for (nuint i = 0; i <= size; i += 250)
        {
            testPositions.Add(i);
        }

        foreach (var pos in testPositions)
        {
            try
            {
                Console.WriteLine($"Querying rank at position {pos}...");
                var rank = compressedBuilderWithRank.RankSetBits(pos);
                Console.WriteLine($"  Rank at {pos}: {rank}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR at position {pos}: {ex.Message}");
                Console.WriteLine($"  Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }

    [Fact]
    public void diagnose_BitsBuilder_size_behavior()
    {
        // Test what happens with BitsBuilder size
        Console.WriteLine("\n=== Testing BitsBuilder Size Behavior ===");
        
        var builder1 = new BitsBuilder(100);
        Console.WriteLine($"BitsBuilder(100) initial size: {builder1.Size}");
        
        builder1.Set(50);
        Console.WriteLine($"After Set(50): {builder1.Size}");
        
        builder1.Set(99);
        Console.WriteLine($"After Set(99): {builder1.Size}");
        
        // Try with random pattern
        var builder2 = new BitsBuilder(1000);
        var rand = new Random(123);
        nuint lastSetPosition = 0;
        for (nuint i = 0; i < 1000; i++)
        {
            if (rand.Next() % 2 != 0)
            {
                builder2.Set(i);
                lastSetPosition = i;
            }
        }
        Console.WriteLine($"BitsBuilder(1000) with random sets:");
        Console.WriteLine($"  Size: {builder2.Size}");
        Console.WriteLine($"  Last set position: {lastSetPosition}");
    }

    [Fact]
    public void diagnose_sample_building()
    {
        Console.WriteLine("\n=== Testing Sample Building ===");
        
        // Create a simple case
        var builder = new BitsBuilder(5000);
        var rand = new Random(456);
        
        for (nuint i = 0; i < 5000; i++)
        {
            if (rand.Next() % 2 != 0)
                builder.Set(i);
        }

        Console.WriteLine($"BitsBuilder size: {builder.Size}");
        
        // Create with rank
        var withRank = new SuccinctCompressedBitsBuilder(builder);
        Console.WriteLine($"WithRank size: {withRank.Size}");
        
        // Try to trigger sample building by querying a position
        try
        {
            Console.WriteLine("Attempting to query rank at position 2016...");
            var rank1 = withRank.RankSetBits(2016);
            Console.WriteLine($"  Success: rank = {rank1}");
            
            Console.WriteLine("Attempting to query rank at position 4032...");
            var rank2 = withRank.RankSetBits(4032);
            Console.WriteLine($"  Success: rank = {rank2}");
            
            Console.WriteLine("Attempting to query rank at position 5000...");
            var rank3 = withRank.RankSetBits(5000);
            Console.WriteLine($"  Success: rank = {rank3}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
            Console.WriteLine($"  Type: {ex.GetType().Name}");
            throw;
        }
    }
}