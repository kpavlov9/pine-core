using System.Runtime.CompilerServices;
using static KGIntelligence.PineCore.Helpers.Utilities.Unions;

namespace KGIntelligence.PineCore.Helpers.Utilities
{
    public static class IONativeBitHelper
    {
        private delegate nuint ReadNativeBits(BinaryReader reader);
        private delegate void WriteNativeBits(BinaryWriter writer, nuint bits);

        private static readonly ReadNativeBits _readNativeBits;
        private static readonly WriteNativeBits _writeNativeBits;

        static IONativeBitHelper()
        {

            if (IntPtr.Size == 4)
            {// 32-Bit System.
                _readNativeBits = static (reader) => reader.ReadUInt32();
                _writeNativeBits = static (writer, bits) => writer.Write(unchecked((uint)bits));
            }
            else
            {// 64-Bit System.
                _readNativeBits = static (reader) => ReadNUInt64(reader);
                _writeNativeBits = static (writer, bits) => WriteNUInt64(bits, writer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint ReadNUInt(this BinaryReader reader) => _readNativeBits(reader);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteNUInt(this BinaryWriter writer, nuint value) => _writeNativeBits(writer, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint ReadNUInt64(
            BinaryReader reader) =>
            (nuint)GetULongLittleEndian(low: reader.ReadUInt32(), high: reader.ReadUInt32());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteNUInt64(
            nuint value,
            BinaryWriter writer)
        {
            GetUIntLittleEndian(value, out var lo, out var hi);
            writer.Write(lo);
            writer.Write(hi);
        }
    }
}
