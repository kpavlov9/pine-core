using KGIntelligence.PineCore.Helpers.Utilities;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.Bits;

/// <summary>
/// Elias-Fano-encoded bits sequence of non-decreasing natural numbers.
/// http://arxiv.org/pdf/1206.4300
/// </summary>
public readonly struct QuasiSuccinctIndices: ISerializableBits<QuasiSuccinctIndices>
{
    private readonly int _lowBitsCount;
    private readonly Bits _lowBits;
    private readonly SuccinctBits.SuccinctBits _highBits;
    private readonly nuint _lowBitsMask;
    private readonly nuint _size;

    public nuint Size => _size;

    public QuasiSuccinctIndices(
        nuint size,
        int lowBitsCount,
        nuint lowBitsMask,
        in Bits lowBits,
        in SuccinctBits.SuccinctBits highBits)
    {
        _size = size;
        _lowBitsCount = lowBitsCount;
        _lowBits = lowBits;
        _highBits = highBits;
        _lowBitsMask = lowBitsMask;
    }

    public nuint Get(nuint position)
    {
        if(position < _size)
        {
            nuint high = _highBits.SelectSetBits(position) - position;
            if (_lowBitsCount == 0) return high;

            nuint lowPosition = position * (nuint)_lowBitsCount;
            nuint low = _lowBits.FetchBits(lowPosition, _lowBitsCount);
            return high << _lowBitsCount | low;
        }

        throw new ArgumentOutOfRangeException(nameof(position));
    }

    public bool Contains(nuint value)
    {
        // Extract high and low parts of the value
        var high = value >> _lowBitsCount;
        var low = value & ((NUIntOne << _lowBitsCount) - 1);

        var highMinusOne = high + 1;

        // Find the range of indices in the high bits that match the high part
        var start = _highBits.SelectSetBits(highMinusOne) - highMinusOne;
        var end = _highBits.SelectSetBits(high) - high;

        // Check the corresponding low parts in the range
        for(var i = start; i < end; i++)
        {
            if(_lowBits.FetchBits(i, _lowBitsCount) == low)
            {
                return true;
            }
        }

        return false;
    }

    public static QuasiSuccinctIndices Read(BinaryReader reader)
    {
        var lowBitsCount = reader.ReadInt32();
        var lowBitsMask = reader.ReadNUInt();
        var position = reader.ReadNUInt();

        var lowBits = Bits.Read(reader);
        var highBits = SuccinctBits.SuccinctBits.Read(reader);

        return new QuasiSuccinctIndices(
            size: position,
            lowBitsCount: lowBitsCount,
            lowBitsMask: lowBitsMask,
            lowBits: lowBits,
            highBits: highBits);
    }

    public static QuasiSuccinctIndices Read(string filename)
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

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var bits = (QuasiSuccinctIndices)obj;
        return 
            _size == bits._size &&
            _lowBits.Equals(bits._lowBits) &&
            _highBits.Equals(bits._highBits);
    }

    public override int GetHashCode()
        => _lowBits.GetHashCode() * _highBits.GetHashCode();

    public static bool operator ==(QuasiSuccinctIndices left, QuasiSuccinctIndices right)
        => left.Equals(right);

    public static bool operator !=(QuasiSuccinctIndices left, QuasiSuccinctIndices right)
        => !(left == right);
}
