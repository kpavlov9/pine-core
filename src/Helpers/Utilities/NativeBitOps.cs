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
        private static readonly Func<nuint, int, bool, int, int> _rankOfReversed;
        private static readonly Func<nuint, int, bool, uint> _selectOfReversed;
        private static readonly Func<nuint, int, bool, uint> _select;
        private static readonly Func<nuint, int, bool, int, int> _rank;

        static NativeBitOps()
        {
            if (Is32BitSystem)
            {
                NativeBitCount = IntBitSize;
                NativeBitCountMinusOne = IntBitSizeMinusOne;

                _reverseBits = static (n) =>
                    unchecked(BitOps.ReverseBits((uint)n));
                _maskedPopCount = static (value, offset) =>
                    unchecked(BitOps.MaskedPopCount((uint)value, (uint)offset));
                _rankOfReversed = static (value, position, forSetBits, blockSize) =>
                    unchecked(BitOps.RankOfReversed((uint)value, position, forSetBits, blockSize));
                _selectOfReversed = static (x, position, forSetBits) =>
                    unchecked(BitOps.SelectOfReversed((uint)x, position, forSetBits));
                _select = static (value, i, forSetBits) =>
                    unchecked(BitOps.Select((uint)value, i, forSetBits));
                _rank = static (value, position, forSetBits, blockSize) =>
                   unchecked(BitOps.Rank((uint)value, position, forSetBits, blockSize));
            }
            else
            {// 64-Bit System.
                NativeBitCount = LongBitSize;
                NativeBitCountMinusOne = LongBitSizeMinusOne;

                _reverseBits = static (n) =>
                    unchecked((nuint)BitOps.ReverseBits(n));
                _maskedPopCount = static (value, offset) =>
                    unchecked(BitOps.MaskedPopCount(value, offset));
                _rankOfReversed = static (value, position, forSetBits, blockSize) =>
                    unchecked(BitOps.RankOfReversed(value, position, forSetBits, blockSize));
                _selectOfReversed = static (x, i, forSetBits) =>
                    unchecked(BitOps.SelectOfReversed(x, i, forSetBits));
                _select = static (value, i, forSetBits) =>
                    unchecked(BitOps.Select(value, i, forSetBits));
                _rank = static (value, position, forSetBits, blockSize) =>
                    unchecked(BitOps.Rank(value, position, forSetBits, blockSize));
            }
        }

        /// <summary>
        /// Returns a value with the reversed bit representation of the input value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint ReverseBits(nuint n) => _reverseBits(n);

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
        /// Returns the count of set or unset bits up to the given position
        /// starting from the least important bit.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RankOfReversed(nuint value, int position, bool forSetBits, int blockSize)
            => _rankOfReversed(value, position, forSetBits, blockSize);

        /// <summary>
        /// Returns the index position of the i-th consecutive set or unset bit
        /// starting from the least important bits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SelectOfReversed(nuint value, int position, bool forSetBits)
            => _selectOfReversed(value, position, forSetBits);

        /// <summary>
        /// Returns the index position of the i-th consecutive set or unset bit
        /// starting from the most important bits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Select(nuint value, int bitCountCutoff, bool forSetBits)
            => _select(value, bitCountCutoff, forSetBits);

        /// <summary>
        /// Returns the count of set or unset bits up to the given position
        /// starting from the most important bit.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Rank(nuint value, int position, bool forSetBits, int blockSize)
            => _rank(value, position, forSetBits, blockSize);
    }
}
