using System.Numerics;
using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;

namespace KGIntelligence.PineCore.Helpers.Utilities
{
    /// <summary>
    /// Collection of bit bit twiddling operations,
    /// which apply on the native-sized binary integers.
    /// This collection is an extension of the collecyion in <see cref="BitOperations"/>
    /// </summary>
    public class NativeBitOps
    {
        public const nuint NUIntOne = 1;
        public const nuint NUIntZero = 0;

        public static readonly byte NativeBitCount;
        public static readonly byte NativeBitCountMinusOne;

        public static readonly bool Is32BitSystem = IntPtr.Size == 4;

        private static readonly Func<nuint, nuint> _reverseBits;
        private static readonly Func<nuint, nuint, uint> _maskedPopCount;
        private static readonly Func<nuint, int, int, int> _rankOfReversed;
        private static readonly Func<nuint, int, uint> _selectOfReversed;
        private static readonly Func<nuint, int, uint> _select;
        private static readonly Func<nuint, int, int, int> _rank;

        static NativeBitOps()
        {
            if (Is32BitSystem)
            {
                NativeBitCount = IntBitSize;
                NativeBitCountMinusOne = IntBitSizeMinusOne;

                _reverseBits = static (value) =>
                    unchecked(BitOps.ReverseBits((uint)value));
                _maskedPopCount = static (value, offset) =>
                    unchecked(BitOps.MaskedPopCount((uint)value, (uint)offset));
                _rankOfReversed = static (value, bitPositionCutoff, blockSize) =>
                    unchecked(BitOps.RankOfReversed((uint)value, bitPositionCutoff, blockSize));
                _selectOfReversed = static (value, bitCountCutoff) =>
                    unchecked(BitOps.SelectOfReversed((uint)value, bitCountCutoff));
                _select = static (value, bitCountCutoff) =>
                    unchecked(BitOps.Select((uint)value, bitCountCutoff));
                _rank = static (value, bitPositionCutoff, blockSize) =>
                   unchecked(BitOps.Rank((uint)value, bitPositionCutoff, blockSize));
            }
            else
            {// 64-Bit System.
                NativeBitCount = LongBitSize;
                NativeBitCountMinusOne = LongBitSizeMinusOne;

                _reverseBits = static (value) =>
                    unchecked((nuint)BitOps.ReverseBits(value));
                _maskedPopCount = static (value, offset) =>
                    unchecked(BitOps.MaskedPopCount(value, offset));
                _rankOfReversed = static (value, bitPositionCutoff, blockSize) =>
                    unchecked(BitOps.RankOfReversed(value, bitPositionCutoff, blockSize));
                _selectOfReversed = static (value, bitCountCutoff) =>
                    unchecked(BitOps.SelectOfReversed(value, bitCountCutoff));
                _select = static (value, bitCountCutoff) =>
                    unchecked(BitOps.Select(value, bitCountCutoff));
                _rank = static (value, bitPositionCutoff, blockSize) =>
                    unchecked(BitOps.Rank(value, bitPositionCutoff, blockSize));
            }
        }

        /// <summary>
        /// Returns a value with the reversed bit representation of the input value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint ReverseBits(nuint value) => _reverseBits(value);

        /// <summary>
        /// Returns the population count (number of bits set) of a mask.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint PopCount(nuint value) =>
            unchecked((uint)BitOperations.PopCount(value));

        /// <summary>
        /// Returns the population count (number of bits set) of a mask.
        /// The mask is adjusted to start the counting from a given offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint MaskedPopCount(nuint value, nuint offset) =>
            _maskedPopCount(value, offset);

        /// <summary>
        /// Returns the count of set bits up to the given position
        /// starting from the least important bit.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RankOfReversed(nuint value, int bitPositionCutoff, int blockSize)
            => _rankOfReversed(value, bitPositionCutoff, blockSize);

        /// <summary>
        /// Returns the index position of the i-th consecutive set bit
        /// starting from the least important bits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SelectOfReversed(nuint value, int bitCountCutoff)
            => _selectOfReversed(value, bitCountCutoff);

        /// <summary>
        /// Returns the index position of the i-th consecutive set bit
        /// starting from the most important bits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Select(nuint value, int bitCountCutoff)
            => _select(value, bitCountCutoff);

        /// <summary>
        /// Returns the count of set bits up to the given position
        /// starting from the most important bit.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Rank(nuint value, int bitPositionCutoff, int blockSize)
            => _rank(value, bitPositionCutoff, blockSize);
    }
}
