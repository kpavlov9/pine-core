﻿using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace KGIntelligence.PineCore.Helpers.Utilities
{
    /// <summary>
    /// Collection of bit bit twiddling operations.
    /// This collection is an extension of the collecyion in <see cref="BitOperations"/>
    /// </summary>
    public static class BitOps
    {
        public const byte BitCountInByte = 8;

        public const byte IntBitSize = 32;
        public const byte LongBitSize = 64;

        public const byte IntBitSizeMinusOne = 31;
        public const byte LongBitSizeMinusOne = 63;
        private const int BitReverseTableLength = 256;
        private static readonly ImmutableArray<byte> BitReverseTable;

        static BitOps()
        {

            Span<byte> bitReverseTable = stackalloc byte[BitReverseTableLength];
            // Precompute the bit-reversal table for 8 bits
            for (ulong i = 0; i < BitReverseTableLength; i++)
            {
                bitReverseTable[(int)i] = (byte)((i * 0x0202020202UL & 0x010884422010UL) % 1023);
            }

            BitReverseTable = ImmutableArray.Create(bitReverseTable);
        }

        /// <summary>
        /// Returns the population count (count of set bits) of a mask.
        /// The mask is adjusted to start the counting from a given offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint MaskedPopCount(ulong value, ulong offset)
            => unchecked((uint)BitOperations.PopCount(value & (1UL << (int)offset) - 1));

        /// <summary>
        /// Returns the population count (count of set bits) of a mask.
        /// The mask is adjusted to start the counting from a given offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint MaskedPopCount(uint value, uint offset) =>
            unchecked((uint)BitOperations.PopCount(value & (uint)(1 << (int)offset) - 1));

        /// <summary>
        /// Returns a value with the reversed bit representation of the input value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReverseBits(byte value)
            => BitReverseTable[value];

        /// <summary>
        /// Returns a value with the reversed bit representation of the input value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReverseBits(ushort value)
            => (ushort)(
                (BitReverseTable[value & 0xFF] << 8) | // Reverse lower byte and shift
                (BitReverseTable[(value >> 8) & 0xFF]) // Reverse upper byte
            );

        /// <summary>
        /// Returns a value with the reversed bit representation of the input value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReverseBits(uint value)
            => (uint)(
                    (BitReverseTable[(int)value & 0xFF] << 24) |
                    (BitReverseTable[(int)(value >> 8) & 0xFF] << 16) |
                    (BitReverseTable[(int)(value >> 16) & 0xFF] << 8) |
                    (BitReverseTable[(int)(value >> 24) & 0xFF])
                );

        /// <summary>
        /// Returns a value with the reversed bit representation of the input value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReverseBits(ulong value)
            => ((ulong)BitReverseTable[(int)value & 0xFF] << 56) |
            ((ulong)BitReverseTable[(int)(value >> 8) & 0xFF] << 48) |
            ((ulong)BitReverseTable[(int)(value >> 16) & 0xFF] << 40) |
            ((ulong)BitReverseTable[(int)(value >> 24) & 0xFF] << 32) |
            ((ulong)BitReverseTable[(int)(value >> 32) & 0xFF] << 24) |
            ((ulong)BitReverseTable[(int)(value >> 40) & 0xFF] << 16) |
            ((ulong)BitReverseTable[(int)(value >> 48) & 0xFF] << 8) |
            BitReverseTable[(int)(value >> 56) & 0xFF];

        /// <summary>
        /// Returns the count of set or unset bits up to the given position
        /// starting from the least important bit.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RankOfReversed(
            ulong value,
            int bitPositionCutoff,
            int blockSize = LongBitSize)
        {
            value <<= blockSize - bitPositionCutoff;
            return BitOperations.PopCount(value);
        }

        /// <summary>
        /// Returns the count of set bits up to the given position
        /// starting from the least important bit.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RankOfReversed(
            uint value,
            int bitPositionCutoff,
            int blockSize = IntBitSize)
        {
            value <<= blockSize - bitPositionCutoff;
            return BitOperations.PopCount(value);
        }


        /// <summary>
        /// Returns the index position of the i-th consecutive set bit
        /// starting from the most important bits.
        /// </summary>
        public static uint Select(ulong value, int bitCountCutoff)
        {
            value = ReverseBits(value);

            ulong pop2 = ((value & 0xaaaaaaaaaaaaaaaaUL) >> 1)
                        + (value & 0x5555555555555555UL);
            ulong pop4 = ((pop2 & 0xccccccccccccccccUL) >> 2)
                        + (pop2 & 0x3333333333333333UL);
            ulong pop8 = ((pop4 & 0xf0f0f0f0f0f0f0f0UL) >> 4)
                        + (pop4 & 0x0f0f0f0f0f0f0f0fUL);
            ulong pop16 = ((pop8 & 0xff00ff00ff00ff00UL) >> 8)
                        + (pop8 & 0x00ff00ff00ff00ffUL);
            ulong pop32 = ((pop16 & 0xffff0000ffff0000UL) >> 16)
                        + (pop16 & 0x0000ffff0000ffffUL);
            ulong pop64 = (pop32 >> 32 & 0x000000000000ffffUL)
                        + (pop32 & 0x000000000000ffffUL);

            ulong iUL = (ulong)bitCountCutoff;

            if (iUL++ >= pop64)
            {// Edge case when first 32 bits are set and 'i' is 31.
                return 63u;
            }

            int pos = 0;
            ulong temp;

            temp = pop32 & 0xffffffffUL;
            if (iUL > temp) { iUL -= temp; pos += 32; }
            temp = pop16 >> pos & 0x0000ffffUL;
            if (iUL > temp) { iUL -= temp; pos += 16; }
            temp = pop8 >> pos & 0x000000ffUL;
            if (iUL > temp) { iUL -= temp; pos += 8; }
            temp = pop4 >> pos & 0x0000000fUL;
            if (iUL > temp) { iUL -= temp; pos += 4; }
            temp = pop2 >> pos & 0x00000003UL;
            if (iUL > temp) { iUL -= temp; pos += 2; }
            temp = value >> pos & 0x00000001UL;
            if (iUL > temp) { pos += 1; }
            return (uint)pos;
        }


        /// <summary>
        /// Returns the index position of the i-th consecutive set bit
        /// starting from the least important bits.
        /// </summary>
        public static uint SelectOfReversed(ulong value, int bitCountCutoff)
        {
            ulong pop2 = ((value & 0xaaaaaaaaaaaaaaaaUL) >> 1)
                        + (value & 0x5555555555555555UL);
            ulong pop4 = ((pop2 & 0xccccccccccccccccUL) >> 2)
                        + (pop2 & 0x3333333333333333UL);
            ulong pop8 = ((pop4 & 0xf0f0f0f0f0f0f0f0UL) >> 4)
                        + (pop4 & 0x0f0f0f0f0f0f0f0fUL);
            ulong pop16 = ((pop8 & 0xff00ff00ff00ff00UL) >> 8)
                        + (pop8 & 0x00ff00ff00ff00ffUL);
            ulong pop32 = ((pop16 & 0xffff0000ffff0000UL) >> 16)
                        + (pop16 & 0x0000ffff0000ffffUL);
            ulong pop64 = (pop32 >> 32 & 0x000000000000ffffUL)
                        + (pop32 & 0x000000000000ffffUL);

            ulong iUL = (ulong)bitCountCutoff;

            if (iUL++ >= pop64)
            {// Edge case when first 32 bits are set and 'i' is 31.
                return 31u;
            }

            int pos = 0;
            ulong temp;

            temp = pop32 & 0xffffffffUL;
            if (iUL > temp) { iUL -= temp; pos += 32; }
            temp = pop16 >> pos & 0x0000ffffUL;
            if (iUL > temp) { iUL -= temp; pos += 16; }
            temp = pop8 >> pos & 0x000000ffUL;
            if (iUL > temp) { iUL -= temp; pos += 8; }
            temp = pop4 >> pos & 0x0000000fUL;
            if (iUL > temp) { iUL -= temp; pos += 4; }
            temp = pop2 >> pos & 0x00000003UL;
            if (iUL > temp) { iUL -= temp; pos += 2; }
            temp = value >> pos & 0x00000001UL;
            if (iUL > temp) { pos += 1; }
            return (uint)pos;
        }

        /// <summary>
        /// Returns the index position of the i-th consecutive set bit
        /// starting from the most important bits.
        /// </summary>
        public static uint Select(uint value, int bitCountCutoff)
        {
            value = ReverseBits(value);

            uint pop2 = (value & 0x55555555u) + (value >> 1 & 0x55555555u);
            uint pop4 = (pop2 & 0x33333333u) + (pop2 >> 2 & 0x33333333u);
            uint pop8 = (pop4 & 0x0f0f0f0fu) + (pop4 >> 4 & 0x0f0f0f0fu);
            uint pop16 = (pop8 & 0x00ff00ffu) + (pop8 >> 8 & 0x00ff00ffu);
            uint pop32 = (pop16 & 0x000000ffu) + (pop16 >> 16 & 0x000000ffu);

            uint iUI = (uint)bitCountCutoff;

            if (iUI++ >= pop32)
            {// Edge case when first 16 bits are set and 'i' is 15.
                return 31u;
            }

            int pos = 0;
            uint temp;

            temp = pop16 & 0xffu;
            if (iUI > temp) { iUI -= temp; pos += 16; }

            temp = pop8 >> pos & 0xffu;
            if (iUI > temp) { iUI -= temp; pos += 8; }

            temp = pop4 >> pos & 0x0fu;
            if (iUI > temp) { iUI -= temp; pos += 4; }

            temp = pop2 >> pos & 0x03u;
            if (iUI > temp) { iUI -= temp; pos += 2; }

            temp = value >> pos & 0x01u;
            if (iUI > temp) { pos += 1; }

            return (uint)pos;
        }

        /// <summary>
        /// Returns the index position of the i-th consecutive set bit
        /// starting from the least important bits.
        /// </summary>
        /// <param name="value">The value in which we do the counting.</param>
        /// <param name="bitCountCutoff">The index position up to which we count the bits.</param>
        public static uint SelectOfReversed(uint value, int bitCountCutoff)
        {
            uint pop2 = (value & 0x55555555u) + (value >> 1 & 0x55555555u);
            uint pop4 = (pop2 & 0x33333333u) + (pop2 >> 2 & 0x33333333u);
            uint pop8 = (pop4 & 0x0f0f0f0fu) + (pop4 >> 4 & 0x0f0f0f0fu);
            uint pop16 = (pop8 & 0x00ff00ffu) + (pop8 >> 8 & 0x00ff00ffu);
            uint pop32 = (pop16 & 0x000000ffu) + (pop16 >> 16 & 0x000000ffu);

            uint iUI = (uint)bitCountCutoff;

            if (iUI++ >= pop32)
            {// Edge case when first 16 bits are set and 'i' is 15.
                return 15u;
            }

            int pos = 0;
            uint temp;

            temp = pop16 & 0xffu;
            if (iUI > temp) { iUI -= temp; pos += 16; }

            temp = pop8 >> pos & 0xffu;
            if (iUI > temp) { iUI -= temp; pos += 8; }

            temp = pop4 >> pos & 0x0fu;
            if (iUI > temp) { iUI -= temp; pos += 4; }

            temp = pop2 >> pos & 0x03u;
            if (iUI > temp) { iUI -= temp; pos += 2; }

            temp = value >> pos & 0x01u;
            if (iUI > temp) { pos += 1; }

            return (uint)pos;
        }

        /// <summary>
        /// Returns the count of set bits up to the given position
        /// starting from the most important bit.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Rank(
            ulong value,
            int bitPositionCutoff,
            int blockSize = LongBitSize) =>
            BitOperations.PopCount(value >>= blockSize - bitPositionCutoff);

        /// <summary>
        /// Returns the count of set bits up to the given position
        /// starting from the most important bit.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Rank(
            uint value,
            int bitPositionCutoff,
            int blockSize = IntBitSize) =>
            BitOperations.PopCount(value >> blockSize - bitPositionCutoff);
    }
}
