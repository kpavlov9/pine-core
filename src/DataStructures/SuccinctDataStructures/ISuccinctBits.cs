namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures;

public interface ISuccinctBits : IBits
{
    /// <summary>
    /// Returns the count of set bits up to the given position
    /// starting from the most important bit.
    /// </summary>
    public nuint RankSetBits(nuint bitPositionCutoff);

    /// <summary>
    /// Returns the count of unset bits up to the given position
    /// starting from the most important bit.
    /// </summary>
    public nuint RankUnsetBits(nuint bitPositionCutoff);

    /// <summary>
    /// Returns the index position of the i-th consecutive set bit
    /// starting from the most important bits.
    /// </summary>
    public nuint SelectSetBits(nuint bitCountCutoff);

    /// <summary>
    /// Returns the index position of the i-th consecutive unset bit
    /// starting from the most important bits.
    /// </summary>
    public nuint SelectUnsetBits(nuint bitCountCutoff);

    /// <summary>
    /// Returns the count of the set bits.
    /// </summary>
    public nuint SetBitsCount { get; }

    /// <summary>
    /// Returns the count of the unset bits.
    /// </summary>
    public nuint UnsetBitsCount { get; }
}
