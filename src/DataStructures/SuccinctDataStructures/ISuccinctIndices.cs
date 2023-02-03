namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures
{
    public interface ISuccinctIndices
    {

        /// <summary>
        /// Returns the count of set bits up to the given position
        /// starting from the most important bit.
        /// </summary>
        nuint RankSetBits(nuint bitPositionCutoff);

        /// <summary>
        /// Returns the count of unset bits up to the given position
        /// starting from the most important bit.
        /// </summary>
        nuint RankUnsetBits(nuint bitPositionCutoff);

        /// <summary>
        /// Returns the index position of the i-th consecutive set bit
        /// starting from the most important bits.
        /// </summary>
        nuint SelectSetBits(nuint bitCountCutoff);

        /// <summary>
        /// Returns the index position of the i-th consecutive unset bit
        /// starting from the most important bits.
        /// </summary>
        nuint SelectUnsetBits(nuint bitCountCutoff);

        /// <summary>
        /// Returns the count of the set bits.
        /// </summary>
        public nuint SetBitsCount { get; }

        /// <summary>
        /// Returns the count of the unset bits.
        /// </summary>
        public nuint UnsetBitsCount { get; }
    }
}
