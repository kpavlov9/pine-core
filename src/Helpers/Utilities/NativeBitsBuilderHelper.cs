using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.Unions;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.Bits;

namespace KGIntelligence.PineCore.Helpers.Utilities
{
    internal static class NativeBitsBuilderHelper
    {
        private static readonly Action<BitsBuilder, ulong, int> _push;

        /// <summary>
        /// Push method for 64-bit systems where nuint == ulong.
        /// Can directly cast and push the value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Push64(BitsBuilder builder, ulong bits, int bitsCount) =>
            builder.AddBits(unchecked((nuint)bits), bitsCount);

        /// <summary>
        /// Push method for 32-bit systems where nuint == uint.
        /// Must split the 64-bit value into two 32-bit parts.
        /// </summary>
        private static void Push32(BitsBuilder builder, ulong bits, int bitsCount)
        {
            // Split the 64-bit value into low and high 32-bit parts
            GetUIntLittleEndian(bits, out var lo, out var hi);
            
            if (bitsCount <= 32)
            {
                // If we need 32 bits or less, just push the low part
                builder.AddBits(unchecked(lo), bitsCount);
            }
            else
            {
                // If we need more than 32 bits, push low part (32 bits) then high part
                builder.AddBits(unchecked(lo), 32);
                builder.AddBits(unchecked(hi), bitsCount - 32);
            }
        }

        static NativeBitsBuilderHelper()
        {
            // FIXED: The logic was backwards before
            if (IntPtr.Size == 4)
            {
                // 32-Bit System: nuint is 32 bits, so we need to split 64-bit values
                _push = Push32;
            }
            else
            {
                // 64-Bit System: nuint is 64 bits, so we can directly cast
                _push = Push64;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushNative(this BitsBuilder builder, ulong bits, int bitsCount)
            => _push(builder, bits, bitsCount);
    }
}