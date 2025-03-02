namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures;

public interface IBits
{
    /// <summary>
    /// The total count of the bits.
    /// </summary>
    public nuint Size { get; }

    /// <summary>
    /// Get the boolean value of the bit the the giver index.
    /// If the value is '<see cref="true"/>' the bit is 1 if the value is
    /// '<see cref="false"/>' the bit is 0.
    /// </summary>
    bool GetBit(nuint index);
}
