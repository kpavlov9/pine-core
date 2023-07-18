namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures;

public interface IBits
{
    /// <summary>
    /// The total count of the bits.
    /// </summary>
    public nuint Size { get; }

    IEnumerable<nuint> Data { get;  }
}
