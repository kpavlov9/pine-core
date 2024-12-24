using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;

namespace KGIntelligence.PineCore.Helpers.Utilities
{
    internal static class SuccinctOps
    {
        internal const int BlockRate = 8;
        internal static readonly int SmallBlockSize = NativeBitCount;
        internal static readonly int LargeBlockSize = SmallBlockSize * BlockRate;
        internal static readonly uint BytesCountInValue = (uint)NativeBitCount / BitCountInByte;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GetBlockPositions(
            nuint position,
            out int qSmall,
            out int rSmall)
        {
            qSmall = (int)position / SmallBlockSize;
            rSmall = (int)position % SmallBlockSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GetMask(int rSmall, out nuint mask) =>
            mask = NUIntOne << rSmall;

        internal static void ValidatePosition(
            nuint position,
            nuint size)
        {
            if (position >= size)
            {
                throw new IndexOutOfRangeException(
                    $@"The argument {nameof(position)} exceeds the sequence length {size}");
            }
        }

        internal static void ValidateFetchBits(int bitsCount)
        {
            if (bitsCount < 0 || bitsCount > NativeBitCount)
            {
                throw new ArgumentOutOfRangeException(
                    $"The given bits count '{bitsCount}' exceeds the valid range: [0, nuint size]."
                );
            }
        }
    }
}

