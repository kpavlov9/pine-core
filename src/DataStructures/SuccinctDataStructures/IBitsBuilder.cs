namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures;

public interface IBitsBuilder
{
    public void Set(nuint position);
    public void Unset(nuint position);

    public nuint Size { get; }

    public IBits BuildBits();
    public ISuccinctBits BuildSuccinctBits();

    public ISuccinctCompressedBits BuildSuccinctCompressedBits();

    public void Clear();

    public IBits ClearAndBuildBits(IBitsContainer bits);
    public ISuccinctBits ClearAndBuildSuccinctBits(IBitsContainer bits);

    public ISuccinctCompressedBits ClearAndBuildSuccinctCompressedBits(IBitsContainer bits);

    public IBitsBuilder Clone();
}
