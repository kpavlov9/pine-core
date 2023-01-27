namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures
{
    public interface ISuccinctIndices
    {

        /// <summary>
        /// Returns the count of set or unset bits up to the given position
        /// starting from the most important bit.
        /// </summary>
        nuint Rank(nuint bitPositionCutoff, bool forSetBits);

        /// <summary>
        /// Returns the index position of the i-th consecutive set or unset bit
        /// starting from the most important bits.
        /// </summary>
        nuint Select(nuint bitCountCutoff, bool forSetBits);

        /// <summary>
        /// Returns the count of the set or unset bits.
        /// </summary>
        public nuint GetBitsCount(bool forSetBits);
    }
}
