namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures;

public interface IBitsBuilder
{
    public void Set(nuint position);
    public void Unset(nuint position);

    public nuint Size { get; }

    public IBitIndices BuildBitIndices();
    public ISuccinctIndices BuildSuccinctBits();

    public ISuccinctCompressedIndices BuildSuccinctCompressedIndices();

    public void Clear();

    public IBitIndices ClearAndBuildBitIndices(IBits bits);
    public ISuccinctIndices ClearAndBuildSuccinctIndices(IBits bits);

    public ISuccinctCompressedIndices ClearAndBuildSuccinctCompressedIndices(IBits bits);

    public IBitsBuilder Clone();
}
