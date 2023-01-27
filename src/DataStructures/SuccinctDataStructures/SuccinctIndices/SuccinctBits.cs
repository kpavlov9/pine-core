using System;
using System.IO;
using System.Linq;
using System.Collections.Immutable;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.Unions;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using static KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices.Bits;
using static KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices.SuccinctBitsBuilder;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices
{
    /// <summary>
    /// Not compressed bit sequence offering rank and select queries.
    /// </summary>
    public readonly struct SuccinctBits : IBitIndices<SuccinctBits>, ISuccinctIndices
    {
        private readonly ImmutableArray<nuint> _values;
        private readonly ImmutableArray<nuint> _ranks;
        private readonly nuint _size;
        private readonly nuint _setBitsCount;

        public nuint Size => _size;

        public SuccinctBits(
            nuint size,
            nuint setBitsCount,
            ImmutableArray<nuint> values,
            ImmutableArray<nuint> ranks)
        {
            _size = size;
            _setBitsCount = setBitsCount;
            _values = values;
            _ranks = ranks;
        }

        public bool GetBit(nuint i)
        {
            if (i >= _size)
            {
                throw new IndexOutOfRangeException(
                    @$"The argument '{nameof(i)}' with value '{i}'
 exceeds the vector length {_size}."
                    );
            }

            int qSmall = (int)i / SmallBlockSize;
            int rSmall = (int)i % SmallBlockSize;
            nuint m = NUIntOne << rSmall;
            return (_values[qSmall] & m) != 0;
        }

        public nuint GetBitsCount(bool forSetBits)
            => forSetBits ? _setBitsCount : _size - _setBitsCount;

        public nuint Rank(nuint i, bool forSetBits)
        {
            if (i > _size)
            {
                throw new IndexOutOfRangeException(
                    @$"The argument '{nameof(i)}' with value '{i}'
 exceeds the vector length {_size}."
                    );
            }

            if (i == 0)
            {
                return 0;
            }

            i--;

            var qLarge = (int)i / LargeBlockSize;
            var qSmall = (int)i / SmallBlockSize;
            int rSmall = (int)i % SmallBlockSize;

            nuint rank = _ranks[qLarge];
            if (!forSetBits)
            {
                rank = (nuint)(qLarge * LargeBlockSize) - rank;
            }

            var begin = qLarge * BlockRate;

            for (var j = begin; j < qSmall; j++)
            {
                rank += unchecked((uint)RankOfReversed(
                    value: _values[j],
                    position: SmallBlockSize,
                    forSetBits: forSetBits,
                    blockSize: SmallBlockSize));
            }

            rank += unchecked((uint)RankOfReversed(
                value: _values[qSmall],
                position: rSmall + 1,
                forSetBits: forSetBits,
                blockSize: SmallBlockSize));

            return rank;
        }

        public nuint Select(nuint i, bool forSetBits)
        {
            if (i >= GetBitsCount(forSetBits))
            {
                throw new ArgumentOutOfRangeException(
                    @$"The argument '{nameof(i)}' with value '{i}' exceeds
 the vector length {GetBitsCount(forSetBits)}."
                );
            }

            var ranks = _ranks;
            int left = 0;
            int right = ranks.Length;
            while (left < right)
            {
                int pivot = left + right >> 1; // / 2;
                nuint rank = ranks[pivot];
                if (!forSetBits) { rank = (nuint)(pivot * LargeBlockSize) - rank; }
                if (i < rank) { right = pivot; }
                else { left = pivot + 1; }
            }
            right--;

            if (forSetBits) { i -= ranks[right]; }
            else { i -= (nuint)(right * LargeBlockSize) - ranks[right]; }
            int j = right * BlockRate;
            while (true)
            {
                uint rank = unchecked((uint)RankOfReversed(
                    _values[j],
                    SmallBlockSize,
                    forSetBits,
                    SmallBlockSize));

                if (i < rank) { break; }
                j++;
                i -= rank;
            }

            return (nuint)(j * SmallBlockSize + SelectOfReversed(
                _values[j],
                (int)i,
                forSetBits));
        }


        public static SuccinctBits Read(BinaryReader reader)
        {
            var size = reader.ReadUInt32();
            var setBitsCount = reader.ReadUInt32();

            var vSize = reader.ReadInt32();
            Span<nuint> values = stackalloc nuint[vSize];

            ReadNUInt(
                buffer: values,
                reader: reader);

            var rSize = reader.ReadInt32();
            Span<nuint> ranks = stackalloc nuint[rSize];

            ReadNUInt(
                buffer: ranks,
                reader: reader);

            return new SuccinctBits(
                size,
                setBitsCount,
                values.ToImmutableArray(),
                ranks.ToImmutableArray());
        }

        public static SuccinctBits Read(string filename)
        {
            using var reader = new BinaryReader(
                new FileStream(
                    filename,
                    FileMode.Open,
                    FileAccess.Read));
            return Read(reader);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(GetUInt(_size));
            writer.Write(GetUInt(_setBitsCount));

            var vSize = _values.Length;

            writer.Write(vSize);

            WriteNUInt(
                buffer: _values,
                cutoff: vSize,
                writer: writer);

            var rSize = _ranks.Length;

            writer.Write(rSize);

            WriteNUInt(
                buffer: _ranks,
                cutoff: rSize,
                writer: writer);
        }

        public void Write(string filename)
        {
            using var writer = new BinaryWriter(
                new FileStream(filename, FileMode.Create, FileAccess.Write));
            Write(writer);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var bits = (SuccinctBits)obj;
            return _size == bits._size && Enumerable.SequenceEqual(
                _values,
                bits._values);
        }

        public override int GetHashCode()
        {
            nuint sum = 19;
            foreach (nuint v in _values)
            {
                sum += 31 * v;
            }
            return (int)sum;
        }

        public static bool operator ==(SuccinctBits left, SuccinctBits right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SuccinctBits left, SuccinctBits right)
        {
            return !(left == right);
        }
    }
}
