using System;
using System.IO;
using System.Linq;
using System.Collections.Immutable;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.Unions;
using static KGIntelligence.PineCore.Helpers.Utilities.SuccinctOps;
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

        public bool GetBit(nuint position)
        {
            ValidatePosition(
                position: position,
                size: _size);

            GetBlockPositions(
                position,
                out int qSmall,
                out int rSmall);

            GetMask(rSmall, out var mask);
            return (_values[qSmall] & mask) != 0;
        }

        public nuint SetBitsCount
            => _setBitsCount;

        public nuint UnsetBitsCount
            => _size - _setBitsCount;

        public nuint RankSetBits(nuint bitPositionCutoff)
        {
            ValidatePosition(position: bitPositionCutoff, size: _size);

            nuint rank = 0;

            if (bitPositionCutoff == 0)
            {
                return rank;
            }

            bitPositionCutoff--;

            CalculateInitialRank(
                values: _values,
                ranks: _ranks,
                bitPositionCutoff: bitPositionCutoff,
                rank: ref rank,
                qLarge: out var qLarge);

            CalculateRankSetBits(
                values: _values,
                bitPositionCutoff: bitPositionCutoff,
                qLarge: qLarge,
                rank: ref rank);

            return rank;
        }

        public nuint RankUnsetBits(nuint bitPositionCutoff)
        {
            ValidatePosition(position: bitPositionCutoff, size: _size);

            nuint rank = 0;

            if (bitPositionCutoff == 0)
            {
                return rank;
            }

            bitPositionCutoff--;

            CalculateInitialRank(
                values: _values,
                ranks: _ranks,
                bitPositionCutoff: bitPositionCutoff,
                rank: ref rank,
                qLarge: out var qLarge);

            CalculateRankUnsetBits(
                values: _values,
                bitPositionCutoff: bitPositionCutoff,
                qLarge: qLarge,
                rank: ref rank);

            return rank;
        }

        private static void CalculateInitialRank(
            ImmutableArray<nuint> values,
            ImmutableArray<nuint> ranks,
            nuint bitPositionCutoff,
            ref nuint rank,
            out int qLarge)
        {
            qLarge = (int)bitPositionCutoff / LargeBlockSize;
            rank = ranks[qLarge];
        }

        private static void CalculateRankSetBits(
            ImmutableArray<nuint> values,
            nuint bitPositionCutoff,
            int qLarge,
            ref nuint rank)
        {
            GetBlockPositions(
                 position: bitPositionCutoff,
                 qSmall: out var qSmall,
                 rSmall: out var rSmall);

            var begin = qLarge * BlockRate;

            for (var j = begin; j < qSmall; j++)
            {
                rank += unchecked((uint)RankOfReversed(
                    value: values[j],
                    bitPositionCutoff: SmallBlockSize,
                    blockSize: SmallBlockSize));
            }

            rank += unchecked((uint)RankOfReversed(
                value: values[qSmall],
                bitPositionCutoff: rSmall + 1,
                blockSize: SmallBlockSize));
        }

        private static void CalculateRankUnsetBits(
            ImmutableArray<nuint> values,
            nuint bitPositionCutoff,
            int qLarge,
            ref nuint rank)
        {
            GetBlockPositions(
                position: bitPositionCutoff,
                qSmall: out var qSmall,
                rSmall: out var rSmall);

            var begin = qLarge * BlockRate;

            rank = (nuint)(qLarge * LargeBlockSize) - rank;

            for (var j = begin; j < qSmall; j++)
            {
                rank += unchecked((uint)RankOfReversed(
                    value: ~values[j],
                    bitPositionCutoff: SmallBlockSize,
                    blockSize: SmallBlockSize));
            }

            rank += unchecked((uint)RankOfReversed(
                value: ~values[qSmall],
                bitPositionCutoff: rSmall + 1,
                blockSize: SmallBlockSize));
        }

        public nuint SelectSetBits(nuint bitCountCutoff)
        {
            ValidateBitCountCutoff(bitCountCutoff, SetBitsCount);

            var ranks = _ranks;
            int left = 0;
            int right = ranks.Length;
            while (left < right)
            {
                int pivot = left + right >> 1; // / 2;
                nuint rank = ranks[pivot];

                if (bitCountCutoff < rank)
                {
                    right = pivot;
                }
                else
                {
                    left = pivot + 1;
                }
            }
            right--;

            bitCountCutoff -= ranks[right];
            int j = right * BlockRate;
            while (true)
            {
                uint rank = unchecked((uint)RankOfReversed(
                    _values[j],
                    SmallBlockSize,
                    SmallBlockSize));

                if (bitCountCutoff < rank)
                {
                    break;
                }
                j++;
                bitCountCutoff -= rank;
            }

            return (nuint)(j * SmallBlockSize + SelectOfReversed(
                _values[j],
                (int)bitCountCutoff));
        }


        public nuint SelectUnsetBits(nuint bitCountCutoff)
        {
            ValidateBitCountCutoff(bitCountCutoff, UnsetBitsCount);

            var ranks = _ranks;
            int left = 0;
            int right = ranks.Length;
            while (left < right)
            {
                int pivot = left + right >> 1; // / 2;
                nuint rank = ranks[pivot];
                rank = (nuint)(pivot * LargeBlockSize) - rank;
                if (bitCountCutoff < rank)
                {
                    right = pivot;
                }
                else
                {
                    left = pivot + 1;
                }
            }
            right--;

            bitCountCutoff -= (nuint)(right * LargeBlockSize) - ranks[right];
            int j = right * BlockRate;
            while (true)
            {
                uint rank = unchecked((uint)RankOfReversed(
                    ~_values[j],
                    SmallBlockSize,
                    SmallBlockSize));

                if (bitCountCutoff < rank) { break; }
                j++;
                bitCountCutoff -= rank;
            }

            return (nuint)(j * SmallBlockSize + SelectOfReversed(
                ~_values[j],
                (int)bitCountCutoff));
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
