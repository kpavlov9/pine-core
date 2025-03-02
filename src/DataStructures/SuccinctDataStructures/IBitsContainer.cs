namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures;

public interface IBitsContainer
{
    /// <summary>
    /// The total count of the bits.
    /// </summary>
    public nuint Size { get; }

    IEnumerable<nuint> Data { get;  }
}
