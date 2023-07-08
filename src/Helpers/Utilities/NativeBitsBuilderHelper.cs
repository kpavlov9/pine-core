using System.Runtime.CompilerServices;

using static KGIntelligence.PineCore.Helpers.Utilities.Unions;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;

namespace KGIntelligence.PineCore.Helpers.Utilities
{
    internal static class NativeBitsBuilderHelper
    {
        private static readonly Action<BitsBuilder, ulong, int> _push;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Push64(BitsBuilder builder, ulong bits, int bitsCount) =>
            builder.AddBits(unchecked((nuint)bits), bitsCount);

        private static void Push32(BitsBuilder builder, ulong bits, int bitsCount)
        {
            GetUIntLittleEndian(bits, out var lo, out var hi);
            builder
                .AddBits(unchecked((nuint)lo), bitsCount)
                .AddBits(unchecked((nuint)hi), bitsCount);
        }

        static NativeBitsBuilderHelper()
        {
            if (IntPtr.Size == 4)
            {// 32-Bit System.
                _push = Push64;
            }
            else
            {// 64-Bit System.
                _push = Push32;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushNative(this BitsBuilder builder, ulong bits, int bitsCount)
            => _push(builder, bits, bitsCount);
    }
}
