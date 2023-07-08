using System.Diagnostics;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

using KGIntelligence.PineCore.Helpers.Utilities;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.IONativeBitHelper;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices
{

    /// <summary>
    /// An implementation of a plain bit sequence.
    /// </summary>
    public readonly struct Bits : IBitIndices<Bits>
    {
        private readonly ImmutableArray<nuint> _data;
        private readonly nuint _position;
        public nuint Size => _position;

        public Bits(nuint position, ImmutableArray<nuint> data)
        {
            _position = position;
            _data = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetBit(nuint position)
            => GetBit(
                position: position,
                data: _data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetBit(
            nuint position,
            IReadOnlyList<nuint> data)
        {
            var nUIntSize = NativeBitCount;
            int indexInList = (int)(position / nUIntSize);
            int offset = (int)(position % nUIntSize);
            nuint block = data[indexInList];

            return (block & NUIntOne << NativeBitCountMinusOne - offset) != 0;
        }

        private nuint InternalFetch(nuint position, int bitsCount)
        {
            if (bitsCount == 0)
            {
                return NUIntZero;
            }

            Debug.Assert(bitsCount > 0);

            var nUIntSize = NativeBitCount;

            var left = position;
            var right = left + (nuint)bitsCount - 1;

            var leftIndexInList = (int)(left / nUIntSize);

            if (leftIndexInList >= _data.Length)
            {
                return NUIntZero;
            }

            var offsetInList = (int)(left & NativeBitCountMinusOne);

            var rightIndexInList = (int)(right / nUIntSize);

            if (leftIndexInList != rightIndexInList)
            {
                // LongSizebit boundary is crossed over:
                var bitsCountForLeft = nUIntSize - offsetInList;
                var bitsCountForRight = bitsCount - bitsCountForLeft;

                var leftBits = InternalFetch(position, bitsCountForLeft);
                var rightBits = InternalFetch(position + (nuint)bitsCountForLeft, bitsCountForRight);
                return leftBits << bitsCountForRight | rightBits;
            }
            else
            {
                // No boundary crossing:
                var block = _data[leftIndexInList];
                if (bitsCount == nUIntSize)
                {
                    Debug.Assert(offsetInList == 0);
                    return block;
                }

                var mask = (NUIntOne << bitsCount) - 1;
                var maskShift = nUIntSize - (offsetInList + bitsCount);
                mask <<= maskShift;
                return (block & mask) >> maskShift;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nuint FetchBits(nuint position, int bitsCount)
        {
            if (bitsCount < 0 || bitsCount > NativeBitCount)
            {
                throw new ArgumentOutOfRangeException(
                    $"The given bits count '{bitsCount}' exceeds the valid range: [0, LongSize]."
                );
            }
            return InternalFetch(position, bitsCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nuint FetchBits(nuint position)
            => InternalFetch(
                position: position,
                bitsCount: NativeBitCount,
                data: _data);

        public void Write(BinaryWriter writer)
        {
            writer.WriteNUInt(_position);

            var size =
                (int)((_position + NativeBitCountMinusOne) / NativeBitCount);
            
            writer.Write(size);

            WriteNUInt(
                buffer: _data,
                cutoff: size,
                writer: writer);
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

        public static void ReadNUInt(
            Span<nuint> buffer,
            BinaryReader reader)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = reader.ReadNUInt();
            }
        }

        public static void WriteNUInt(
            IReadOnlyList<nuint> buffer,
            int cutoff,
            BinaryWriter writer)
        {
            for (var i = 0; i < cutoff; i++)
            {
                writer.WriteNUInt(buffer[i]);
            }
        }

        public static Bits Read(BinaryReader reader)
        {
            var position = reader.ReadNUInt();

            var size = reader.ReadInt32();

            Span<nuint> data = stackalloc nuint[size];

            ReadNUInt(data, reader);

            return new Bits(position, data.ToImmutableArray());
        }

        public static Bits Read(string filename)
        {
            using var reader =
                new BinaryReader(
                    new FileStream(
                        filename,
                        FileMode.Open,
                        FileAccess.Read));

            return Read(reader);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var bits = (Bits)obj;

            var validDataRange =
                (int)((_position + NativeBitCountMinusOne) / NativeBitCount);

            return bits._position == _position &&
                    bits._data
                        .Take(validDataRange)
                        .SequenceEqual(_data.Take(validDataRange));
        }

        public override int GetHashCode()
        {
            var sum = (nuint)19;
            foreach (nuint value in _data)
            {
                sum += 31 * value;
            }
            return (int)sum;
        }

        public static bool operator ==(Bits left, Bits right)
            => left.Equals(right);

        public static bool operator !=(Bits left, Bits right)
            => !(left == right);
    }

}
