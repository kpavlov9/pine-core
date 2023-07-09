namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures;

public interface ISerializableBits<TSelf>
    where TSelf : ISerializableBits<TSelf>
{
    /// <summary>
    /// Write the bits to the given writer.
    /// </summary>
    public void Write(BinaryWriter writer);

    /// <summary>
    /// Write the bits to a file with file path.
    /// </summary>
    public void Write(string filePath);

    /// <summary>
    /// Read bits from a binary reader.
    /// </summary>
    public static abstract TSelf Read(BinaryReader reader);

    /// <summary>
    /// Read bits from a binary file with the given file name.
    /// </summary>
    public static abstract TSelf Read(string filePath);
}
