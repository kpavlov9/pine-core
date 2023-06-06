using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices;
using static KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices.BitsBuilder;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices
{
    /// <summary>
    ///  Builds the bits sequence <see cref="QuasiSuccinctBits"/>
    /// by adding bit by bit or an array of bits.
    /// </summary>
    public sealed class QuasiSuccinctBitsBuilder
    {
        private readonly int _lowBitsCount;
        private readonly nuint _lowBitsMask;

        private readonly nuint _upperBound;

        private readonly nuint _maxLength;
        private nuint _position = 0;
        private nuint _lastValue = 0;

        private readonly BitsBuilder _lowBits;
        private readonly SuccinctBitsBuilder _highBitsBuilder;

        public nuint Size => _position;

        public QuasiSuccinctBitsBuilder(int length, int upperBound)
        {
            _maxLength = (nuint)length;
            _upperBound = (nuint)upperBound;

            _lowBitsCount = Math.Max(0, int.Log2(upperBound / length));
            _lowBitsMask = (NUIntOne << _lowBitsCount) - 1;

            nuint numLowBits = (nuint)(length * _lowBitsCount);
            _lowBits = OfFixedLength(numLowBits);

            // Give the theoretical lower bound as the initial capacity
            _highBitsBuilder = new SuccinctBitsBuilder(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QuasiSuccinctBitsBuilder AddBits(nuint value)
        {
            if (_position >= _maxLength)
            {
                throw new InvalidOperationException(
                    $"The number of added elements exceeds the limit of '{_maxLength}'");
            }

            if (_lastValue > value)
            {
                throw new InvalidOperationException(
                    @$"The sequence should be non-decreasing,
 but the last value is '{_lastValue}' and the current one is '{value}'.");
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
            _position++;

            return this;
        }

        public QuasiSuccinctBits Build()
            => new QuasiSuccinctBits(
                size: _position,
                lowBitsCount: _lowBitsCount,
                lowBitsMask: _lowBitsMask,
                lowBits: _lowBits.Build(),
                highBits: _highBitsBuilder.Build());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nuint MaxLength() => _maxLength;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nuint UpperBound() => _upperBound;
    }
}
