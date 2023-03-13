using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.SuccinctOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;

using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices
{
    /// <summary>
    /// Builds the bits sequence <see cref="SuccinctBits"/>
    /// by adding bit by bit or an array of bits.
    /// </summary>
    public sealed class SuccinctBitsBuilder
    {
        private readonly List<nuint> _values;
        private readonly List<nuint> _ranks;
        private nuint _size;
        private nuint _setBitsCount;

        public nuint Size => _size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nuint GetBitsCount(bool forSetBits)
            => forSetBits ? _setBitsCount : _size - _setBitsCount;

        public SuccinctBitsBuilder()
        {
            _values = new List<nuint>();
            _ranks = new List<nuint>();
            _size = 0;
            _setBitsCount = 0;
        }

        public SuccinctBitsBuilder(BitsBuilder bits)
        {
            _values = new List<nuint>(bits.GetData().Count);
            _ranks = new List<nuint>();

            foreach (nuint v in bits.GetData())
            {
                _values.Add(ReverseBits(v));
            }

            _size = bits.Size;
            Build();
        }

        public SuccinctBitsBuilder(nuint size)
        {
            _values =
                new List<nuint>(
                    new nuint[
                        (int)((size + NativeBitCountMinusOne) / NativeBitCount)
                        ]);
            _ranks = new List<nuint>();
            _size = size;
            _setBitsCount = 0;
        }


        public SuccinctBitsBuilder(IReadOnlyList<byte> bytes)
        {
            _values = new List<nuint>();
            _ranks = new List<nuint>();

            nuint value = 0;
            nuint i = 0;
            foreach (byte b in bytes)
            {
                value = value << BitCountInByte | b;
                ++i;
                if (i % BytesCountInValue == 0)
                {
                    _values.Add(ReverseBits(value));
                }
            }
            if (i % BytesCountInValue > 0)
            {
                _values.Add(ReverseBits(value));
                value = 0;
            }
            _size = i * BitCountInByte;
        }

        public SuccinctBitsBuilder Clear()
        {
            _values.Clear();
            _ranks.Clear();
            _size = 0;
            _setBitsCount = 0;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetBit(nuint position)
        {
            if (position >= _size)
            {
                throw new IndexOutOfRangeException(
                    $@"The argument {nameof(position)}
 exceeds the sequence length {_size}");
            }

            GetBlockPositions(
                position,
                out int qSmall,
                out int rSmall);

            GetMask(rSmall, out var mask);
            return (_values[qSmall] & mask) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SuccinctBitsBuilder Set(nuint position)
        {
            if(position >= _size)
            {
                _size = position + 1;
            }

            GetBlockPositions(
                position,
                out int qSmall,
                out int rSmall);

            while (qSmall >= _values.Count)
            {
                _values.Add(0);
            }

            GetMask(rSmall, out var mask);

            _values[qSmall] = _values[qSmall] | mask;

            return this;
        }

        public SuccinctBitsBuilder Unset(nuint position)
        {
            if (position >= _size)
            {
                _size = position + 1;
            }

            GetBlockPositions(
                position,
                out int qSmall,
                out int rSmall);

            while (qSmall >= _values.Count)
            {
                _values.Add(0);
            }

            GetMask(rSmall, out var mask);

            _values[qSmall] = _values[qSmall] & ~mask;

            return this;
        }

        public SuccinctBits Build()
        {
            _ranks.Clear();
            _ranks.Capacity = (_values.Count + BlockRate - 1) / BlockRate;
            _setBitsCount = 0;
            for (var i = 0; i < _values.Count; i++)
            {
                if (i % BlockRate == 0)
                {
                    _ranks.Add(GetBitsCount(forSetBits: true));
                }
                _setBitsCount += unchecked((nuint)RankOfReversed(
                    value: _values[i],
                    bitPositionCutoff: SmallBlockSize,
                    blockSize: SmallBlockSize));
            }

            return new SuccinctBits(
                _size,
                _setBitsCount,
                _values.ToImmutableArray(),
                _ranks.ToImmutableArray());
        }
    }
}
