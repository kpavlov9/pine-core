using KGIntelligence.PineCore.Helpers.Utilities;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;

/// <summary>
/// Elias-Fano-encoded bits sequence of non-decreasing natural numbers.
/// http://arxiv.org/pdf/1206.4300
/// </summary>
public readonly struct QuasiSuccinctBits: ISerializableBits<QuasiSuccinctBits>
{
    private readonly int _lowBitsCount;
    private readonly Bits _lowBits;
    private readonly SuccinctBits _highBits;
    private readonly nuint _lowBitsMask;
    private readonly nuint _size;

    public nuint Size => _size;

    public QuasiSuccinctBits(
        nuint size,
        int lowBitsCount,
        nuint lowBitsMask,
        in Bits lowBits,
        in SuccinctBits highBits)
    {
        _size = size;
        _lowBitsCount = lowBitsCount;
        _lowBits = lowBits;
        _highBits = highBits;
        _lowBitsMask = lowBitsMask;
    }

    public nuint GetBit(nuint position)
    {
        nuint high = _highBits.SelectSetBits(position) - position;
        if (_lowBitsCount == 0) return high;

        nuint lowPosition = position * (nuint)_lowBitsCount;
        nuint low = _lowBits.FetchBits(lowPosition, _lowBitsCount);
        return high << _lowBitsCount | low;
    }

    public static QuasiSuccinctBits Read(BinaryReader reader)
    {
        var lowBitsCount = reader.ReadInt32();

        var lowBitsMask = reader.ReadNUInt();
        var position = reader.ReadNUInt();

        var lowBits = Bits.Read(reader);
        var highBits = SuccinctBits.Read(reader);

        return new QuasiSuccinctBits(
            size: position,
            lowBitsCount: lowBitsCount,
            lowBitsMask: lowBitsMask,
            lowBits: lowBits,
            highBits: highBits);
    }

    public static QuasiSuccinctBits Read(string filename)
    {
        var reader =
            new BinaryReader(
                new FileStream(
                    filename,
                    FileMode.Open,
                    FileAccess.Read));

        return Read(reader);
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(_lowBitsCount);

        writer.WriteNUInt(_lowBitsMask);
        writer.WriteNUInt(_size);

        _lowBits.Write(writer);
        _highBits.Write(writer);
    }

    public void Write(string filename)
    {
        var writer =
            new BinaryWriter(
                new FileStream(
                    filename,
                    FileMode.Create,
                    FileAccess.Write));

        Write(writer);
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var bits = (QuasiSuccinctBits)obj;
        return 
            _size == bits._size &&
            _lowBits.Equals(bits._lowBits) &&
            _highBits.Equals(bits._highBits);
    }

    public override int GetHashCode()
        => _lowBits.GetHashCode() * _highBits.GetHashCode();

    public static bool operator ==(QuasiSuccinctBits left, QuasiSuccinctBits right)
        => left.Equals(right);

    public static bool operator !=(QuasiSuccinctBits left, QuasiSuccinctBits right)
        => !(left == right);
}
