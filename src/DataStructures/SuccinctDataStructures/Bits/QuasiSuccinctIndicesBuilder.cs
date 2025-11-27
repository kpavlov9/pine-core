using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctBits;
using KGIntelligence.PineCore.Helpers.Utilities;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.Bits;

/// <summary>
/// Builds the bits sequence <see cref="QuasiSuccinctIndices"/>
/// by adding bit by bit or an array of bits.
/// Supports querying during construction for improved usability.
/// </summary>
public sealed class QuasiSuccinctBitsBuilder : ISerializableBits<QuasiSuccinctBitsBuilder>
{
    private readonly int _lowBitsCount;
    private readonly nuint _lowBitsMask;

    private readonly nuint _upperBound;
    private readonly nuint _maxLength;
    
    private nuint _position = 0;
    private nuint _lastValue = 0;

    private readonly BitsBuilder _lowBits;
    private readonly SuccinctBitsBuilder _highBitsBuilder;

    // Cache for values to support Get() during building
    private readonly List<nuint>? _valuesCache;
    private readonly bool _enableCache;

    public nuint Size => _position;
    public nuint MaxLength => _maxLength;
    public nuint UpperBound => _upperBound;

    /// <summary>
    /// Creates a new builder for quasi-succinct indices.
    /// </summary>
    /// <param name="length">Maximum number of elements</param>
    /// <param name="upperBound">Maximum value that can be stored</param>
    /// <param name="enableCache">If true, enables Get() and Contains() during building at the cost of memory</param>
    public QuasiSuccinctBitsBuilder(int length, int upperBound, bool enableCache = false)
    {
        _maxLength = (nuint)length;
        _upperBound = (nuint)upperBound;
        _enableCache = enableCache;

        _lowBitsCount = Math.Max(0, int.Log2(upperBound / length));
        _lowBitsMask = (NUIntOne << _lowBitsCount) - 1;

        nuint numLowBits = (nuint)(length * _lowBitsCount);
        _lowBits = new BitsBuilder(numLowBits);

        // Give the theoretical lower bound as the initial capacity
        _highBitsBuilder = new SuccinctBitsBuilder((nuint)length);

        if (_enableCache)
        {
            _valuesCache = new List<nuint>(length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public QuasiSuccinctBitsBuilder Add(nuint value)
    {
        if (_position >= _maxLength)
        {
            throw new InvalidOperationException(
                $"The number of added elements exceeds the limit of '{_maxLength}'");
        }

        if (_lastValue > value)
        {
            throw new InvalidOperationException(
                $"The sequence should be non-decreasing, but the last value is '{_lastValue}' and the current one is '{value}'.");
        }

        if (value > _upperBound)
        {
            throw new InvalidOperationException(
                $"The input value {value} exceeds the upper bound {_upperBound}");
        }

        nuint high = value >> _lowBitsCount;
        nuint low = value & _lowBitsMask;

        _lowBits.AddBits(low, _lowBitsCount);
        _highBitsBuilder.Set(_position + high);

        _lastValue = value;
        
        if (_enableCache && _valuesCache != null)
        {
            _valuesCache.Add(value);
        }

        _position++;

        return this;
    }

    /// <summary>
    /// Gets the value at the specified position.
    /// Only available if cache is enabled during construction.
    /// </summary>
    public nuint Get(nuint position)
    {
        if (position >= _position)
        {
            throw new ArgumentOutOfRangeException(nameof(position), 
                $"Position {position} is out of range. Current size is {_position}.");
        }

        if (_enableCache && _valuesCache != null)
        {
            return _valuesCache[(int)position];
        }

        // Fallback: reconstruct from bits (slower)
        nuint lowPosition = position * (nuint)_lowBitsCount;
        nuint low = _lowBits.FetchBits(lowPosition, _lowBitsCount);
        
        // For high bits, we need to build and use the succinct structure
        var highBits = _highBitsBuilder.Build();
        nuint high = highBits.SelectSetBits(position) - position;
        
        return (high << _lowBitsCount) | low;
    }

    /// <summary>
    /// Checks if a value exists in the builder.
    /// Only available if cache is enabled during construction.
    /// </summary>
    public bool Contains(nuint value)
    {
        if (_position == 0) return false;

        if (_enableCache && _valuesCache != null)
        {
            // Use binary search on cached values (they're sorted)
            return BinarySearch(value) >= 0;
        }

        // Fallback: linear search through Get() (slower)
        for (nuint i = 0; i < _position; i++)
        {
            nuint current = Get(i);
            if (current == value) return true;
            if (current > value) return false; // Since it's sorted
        }

        return false;
    }

    /// <summary>
    /// Performs binary search on cached values.
    /// Returns the index if found, or -1 if not found.
    /// </summary>
    private long BinarySearch(nuint value)
    {
        if (_valuesCache == null) return -1;

        long left = 0;
        long right = (long)_position - 1;

        while (left <= right)
        {
            long mid = left + (right - left) / 2;
            nuint midValue = _valuesCache[(int)mid];

            if (midValue == value)
            {
                return mid;
            }
            else if (midValue < value)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns the index of the value in the builder, or -1 if not found.
    /// Only available if cache is enabled.
    /// </summary>
    public long IndexOf(nuint value)
    {
        if (_enableCache && _valuesCache != null)
        {
            return BinarySearch(value);
        }

        // Fallback: linear search (slower)
        for (nuint i = 0; i < _position; i++)
        {
            nuint current = Get(i);
            if (current == value) return (long)i;
            if (current > value) return -1;
        }

        return -1;
    }

    /// <summary>
    /// Clears all added values and resets the builder.
    /// </summary>
    public void Clear()
    {
        _position = 0;
        _lastValue = 0;
        _lowBits.Clear();
        _highBitsBuilder.Clear();
        _valuesCache?.Clear();
    }

    public QuasiSuccinctIndices Build()
        => new (
            size: _position,
            lowBitsCount: _lowBitsCount,
            lowBitsMask: _lowBitsMask,
            lowBits: _lowBits.Build(),
            highBits: _highBitsBuilder.Build());

    #region Serialization

    public void Write(BinaryWriter writer)
    {
        writer.Write(_lowBitsCount);
        writer.WriteNUInt(_lowBitsMask);
        writer.WriteNUInt(_position);
        writer.WriteNUInt(_maxLength);
        writer.WriteNUInt(_upperBound);
        writer.WriteNUInt(_lastValue);
        
        _lowBits.Write(writer);
        
        // Write the built high bits structure instead of builder
        var highBits = _highBitsBuilder.Build();
        highBits.Write(writer);

        // Write cache if enabled
        writer.Write(_enableCache);
        if (_enableCache && _valuesCache != null)
        {
            writer.Write(_valuesCache.Count);
            foreach (var value in _valuesCache)
            {
                writer.WriteNUInt(value);
            }
        }
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

    public static QuasiSuccinctBitsBuilder Read(BinaryReader reader)
    {
        var lowBitsCount = reader.ReadInt32();
        var lowBitsMask = reader.ReadNUInt();
        var position = reader.ReadNUInt();
        var maxLength = reader.ReadNUInt();
        var upperBound = reader.ReadNUInt();
        var lastValue = reader.ReadNUInt();

        var lowBits = BitsBuilder.Read(reader);
        
        // Read the high bits structure and create a builder from it
        var highBits = SuccinctBits.SuccinctBits.Read(reader);

        var enableCache = reader.ReadBoolean();
        List<nuint>? valuesCache = null;

        if (enableCache)
        {
            var cacheCount = reader.ReadInt32();
            valuesCache = new List<nuint>(cacheCount);
            for (int i = 0; i < cacheCount; i++)
            {
                valuesCache.Add(reader.ReadNUInt());
            }
        }

        // Reconstruct the builder by re-adding values from cache
        var builder = new QuasiSuccinctBitsBuilder(
            (int)maxLength, 
            (int)upperBound, 
            enableCache);

        if (enableCache && valuesCache != null)
        {
            foreach (var value in valuesCache)
            {
                builder.Add(value);
            }
        }
        else if (valuesCache != null)
        {
            // If no cache but we have the cached values, use them
            foreach (var value in valuesCache)
            {
                builder.Add(value);
            }
        }
        else
        {
            // Without cache, we need to reconstruct values from the structures
            // This is slower but works
            for (nuint i = 0; i < position; i++)
            {
                nuint highValue = highBits.SelectSetBits(i) - i;
                nuint lowPosition = i * (nuint)lowBitsCount;
                nuint lowValue = lowBits.FetchBits(lowPosition, lowBitsCount);
                nuint value = (highValue << lowBitsCount) | lowValue;
                builder.Add(value);
            }
        }

        return builder;
    }

    public static QuasiSuccinctBitsBuilder Read(string filename)
    {
        using var reader =
            new BinaryReader(
                new FileStream(
                    filename,
                    FileMode.Open,
                    FileAccess.Read));

        return Read(reader);
    }

    #endregion

    public override bool Equals(object? obj)
    {
        if (obj is not QuasiSuccinctBitsBuilder other) return false;

        if (_position != other._position || 
            _lowBitsCount != other._lowBitsCount ||
            _maxLength != other._maxLength ||
            _upperBound != other._upperBound)
        {
            return false;
        }

        // Compare cached values if both have cache
        if (_enableCache && other._enableCache && 
            _valuesCache != null && other._valuesCache != null)
        {
            return _valuesCache.SequenceEqual(other._valuesCache);
        }

        // Otherwise compare the built structures
        return Build().Equals(other.Build());
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + _position.GetHashCode();
            hash = hash * 31 + _lowBitsCount.GetHashCode();
            hash = hash * 31 + _maxLength.GetHashCode();
            hash = hash * 31 + _upperBound.GetHashCode();
            return hash;
        }
    }
}