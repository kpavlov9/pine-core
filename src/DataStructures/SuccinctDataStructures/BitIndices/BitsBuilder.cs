using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

using KGIntelligence.PineCore.Helpers.Utilities;
using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitsBuilderHelper;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices
{
    /// <summary>
    /// Builds the bits sequence <see cref="Bits"/>
    /// by adding bit by bit or an array of bits.
    /// </summary>
    public sealed class BitsBuilder
    {
        private List<nuint> _data;
        private uint _position = 0;

        public uint Size => _position;

        private static int GetCapacity(ulong initialCapacity) =>
            ((int)initialCapacity + NativeBitCountMinusOne) / NativeBitCount;

        public BitsBuilder(ulong initialCapacity)
        {
            _data = new List<nuint>(new nuint[GetCapacity(initialCapacity)]);
        }

        public BitsBuilder(nuint[] bits)
        {
            _data = new List<nuint>(bits);
        }

        public BitsBuilder(IEnumerable<nuint> bits)
        {
            _data = bits.ToList();
        }

        public BitsBuilder(byte[] bits)
            : this((nuint)bits.Length * BitCountInByte)
        {
            Fill(bits);
        }

        public BitsBuilder(IEnumerable<byte> bits)
            : this(bits.ToArray())
        {
        }

        private void Fill(ReadOnlySpan<byte> bits)
        {
            var marrowBits = MemoryMarshal.Cast<byte, Vector<byte>>(bits);

            //[Hack]: unsafe operation and we shouldn't add any items to the list while operating with the span.
            var marrowData = MemoryMarshal.Cast<nuint, Vector<byte>>(CollectionsMarshal.AsSpan(_data));

            for (var i = 0; i < marrowBits.Length; i++)
            {
                marrowData[i] = marrowBits[i];
            }

            var cutoff = Vector<byte>.Count * marrowBits.Length;

            _position = (uint)(cutoff * BitCountInByte);

            for (var i = cutoff; i < bits.Length; i++)
            {
                Add(bits[i], BitCountInByte);
            }
        }

        public static BitsBuilder OfFixedLength(nuint length)
        {
            return new BitsBuilder(new nuint[GetCapacity(length)]);
        }

        public static BitsBuilder OfFixedLength(byte[] bits)
        {
            var b = OfFixedLength((nuint)(bits.Length * BitCountInByte));
            b.Fill(bits);
            return b;
        }

        public void Clear()
        {
            _data.Clear();
            _position = 0;
        }

        public Bits Build()
        {
            return new Bits(_position, _data.ToImmutableArray());
        }

        public BitsBuilder Set(uint position)
        {
            int indexInList = (int)(position / NativeBitCount);
            int offset = (int)(position % NativeBitCount);
            nuint block = _data[indexInList];
            nuint mask = NUIntOne << NativeBitCountMinusOne - offset;
            block |= mask;
            _data[indexInList] = block;

            if (position + 1 >= _position)
            {
                _position = position + 1;
            }
            return this;
        }

        public BitsBuilder Unset(uint position)
        {
            int indexInList = (int)(position / NativeBitCount);
            int offset = (int)(position % NativeBitCount);
            nuint block = _data[indexInList];
            nuint mask = NUIntOne << NativeBitCountMinusOne - offset;
            block &= ~mask;
            _data[indexInList] = block;

            if (position + 1 >= _position)
            {
                _position = position + 1;
            }
            return this;
        }

        public BitsBuilder AddBits(nuint bits, int bitsCount)
        {
            if (bitsCount == 0)
            {
                return this;
            }

            if (bitsCount < 0 || bitsCount > NativeBitCount)
            {
                throw new ArgumentOutOfRangeException(
                    $"The given bits count of '{bitsCount}' exceeds the valid range: [0, {sizeof(long)}]."
                );
            }

            var left = _position;
            var right = left + (nuint)bitsCount - 1;

            var leftIndexInList = (int)(left / NativeBitCount);
            var offsetInList = (int)(left % NativeBitCount);

            var rightIndexInList = (int)(right / NativeBitCount);

            if (leftIndexInList != rightIndexInList)
            {
                // LongSizebit boundary is crossed over
                var bitsCountForLeft = NativeBitCount - offsetInList;
                var bitsCountForRight = bitsCount - bitsCountForLeft;
                var bitsForLeft = bits >> bitsCountForRight;

                AddBits(bitsForLeft, bitsCountForLeft);
                AddBits(bits, bitsCountForRight);
            }
            else
            {
                // no boundary crossing
                if (leftIndexInList >= _data.Count)
                {
                    _data.Add(NUIntZero);
                }
                var block = _data[leftIndexInList];

                var mask = bitsCount == NativeBitCount
                    ? nuint.MaxValue
                    : (NUIntOne << bitsCount) - NUIntOne;

                bits &= mask;
                bits <<= NativeBitCount - (offsetInList + bitsCount);
                block |= bits;
                _data[leftIndexInList] = block;
                _position += (uint)bitsCount;
            }
            return this;
        }

        public BitsBuilder Add(ulong bits, int bitsCount)
        {
            this.PushNative(bits, bitsCount);
            return this;
        }

        public BitsBuilder Add(uint bits, int bitsCount)
        {
            return AddBits((nuint)bits, bitsCount);
        }

        public BitsBuilder AddBits(IList<byte> values)
        {
            foreach (byte value in values)
            {
                Add(value, sizeof(byte) * 8);
            }
            return this;
        }

        public BitsBuilder AddSetBits(int bitsCount)
        {
            var ones = nuint.MaxValue;
            for (var i = 0; i < bitsCount / NativeBitCount; i++)
            {
                AddBits(ones, NativeBitCount);
            }

            var r = bitsCount % NativeBitCount;

            if (r > 0)
            {
                AddBits(ones, r);
            }

            return this;
        }

        public BitsBuilder AddUnsetBits(int bitsCount)
        {
            const nuint zeros = NUIntZero;
            for (var i = 0; i < bitsCount / NativeBitCount; i++)
            {
                AddBits(zeros, NativeBitCount);
            }

            var r = bitsCount % NativeBitCount;

            if (r > 0)
            {
                AddBits(zeros, r);
            }

            return this;
        }



        public BitsBuilder Add(bool bitValue)
        {
            return bitValue ? AddSetBits(1) : AddUnsetBits(1);
        }

        public IList<nuint> GetData()
        {
            return _data;
        }
    }
}
