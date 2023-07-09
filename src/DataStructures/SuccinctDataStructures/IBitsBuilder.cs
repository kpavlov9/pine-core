namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures;

public interface IBitsBuilder
{
    public void Set(nuint position);
    public void Unset(nuint position);

    public IBitIndices BuildBitIndices();
    public ISuccinctIndices BuildSuccinctIndices();

    public ISuccinctCompressedIndices BuildSuccinctCompressedIndices();

    public void Clear();
}
