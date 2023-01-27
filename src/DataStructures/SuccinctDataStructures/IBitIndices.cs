namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures
{
    public interface IBitIndices<TSelf>
        where TSelf : IBitIndices<TSelf>
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

        /// <summary>
        /// Write the bits to the given writer.
        /// </summary>
        void Write(BinaryWriter writer);

        /// <summary>
        /// Write the bits to a file with file path.
        /// </summary>
        void Write(string filePath);

        /// <summary>
        /// Read bits from a binary reader.
        /// </summary>
        public static abstract TSelf Read(BinaryReader reader);

        /// <summary>
        /// Read bits from a binary file with the given file name.
        /// </summary>
        public static abstract TSelf Read(string filePath);

        /// <summary>
        /// Determines whether two object instances are equal.
        /// </summary>
        public bool Equals(object @object);

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public int GetHashCode();
    }
}
