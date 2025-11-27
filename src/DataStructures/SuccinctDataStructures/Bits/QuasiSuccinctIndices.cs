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

    /// <summary>
    /// Checks if a value exists in the quasi-succinct indices using binary search on high bits.
    /// </summary>
    public bool Contains(nuint value)
    {
        if (_size == 0) return false;

        // Extract high and low parts of the value
        var high = value >> _lowBitsCount;
        var low = _lowBitsCount > 0 ? value & _lowBitsMask : 0;

        // Find positions where high bits match
        // For Elias-Fano encoding, the i-th element has high bits at position i + high_value
        // So we need to find all positions where SelectSetBits(i) - i == high

        // Binary search to find the range of indices with matching high bits
        nuint start = 0;
        nuint end = _size;

        // Find first position where high bits >= target high
        while (start < end)
        {
            nuint mid = start + (end - start) / 2;
            nuint midHigh = _highBits.SelectSetBits(mid) - mid;
            
            if (midHigh < high)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }

        // If no position found or we're out of bounds
        if (start >= _size)
        {
            return false;
        }

        // Check all positions with matching high bits
        for (nuint i = start; i < _size; i++)
        {
            nuint currentHigh = _highBits.SelectSetBits(i) - i;
            
            // If we've passed the target high value, it doesn't exist
            if (currentHigh > high)
            {
                return false;
            }

            // If high bits match, check low bits
            if (currentHigh == high)
            {
                if (_lowBitsCount == 0)
                {
                    return true; // No low bits to check
                }

                nuint lowPosition = i * (nuint)_lowBitsCount;
                nuint currentLow = _lowBits.FetchBits(lowPosition, _lowBitsCount);
                
                if (currentLow == low)
                {
                    return true;
                }
                
                // Since values are sorted, if currentLow > low, value doesn't exist
                if (currentLow > low)
                {
                    return false;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Performs binary search to find the index of a value, or -1 if not found.
    /// </summary>
    public long IndexOf(nuint value)
    {
        if (_size == 0) return -1;

        nuint left = 0;
        nuint right = _size - 1;

        while (left <= right)
        {
            nuint mid = left + (right - left) / 2;
            nuint midValue = Get(mid);

            if (midValue == value)
            {
                return (long)mid;
            }
            else if (midValue < value)
            {
                left = mid + 1;
            }
            else
            {
                if (mid == 0) break;
                right = mid - 1;
            }
        }

        return -1;
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
        using var reader =
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
        using var writer =
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
            _lowBitsCount == bits._lowBitsCount &&
            _lowBitsMask == bits._lowBitsMask &&
            _lowBits.Equals(bits._lowBits) &&
            _highBits.Equals(bits._highBits);
    }

    public override int GetHashCode()
    {
        // Better hash code implementation
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + _size.GetHashCode();
            hash = hash * 31 + _lowBitsCount.GetHashCode();
            hash = hash * 31 + _lowBits.GetHashCode();
            hash = hash * 31 + _highBits.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(QuasiSuccinctIndices left, QuasiSuccinctIndices right)
        => left.Equals(right);

    public static bool operator !=(QuasiSuccinctIndices left, QuasiSuccinctIndices right)
        => !(left == right);
}