using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;

namespace KGIntelligence.PineCore.Helpers.Utilities
{
    /// <summary>
    /// Be careful by using the unions for data persistance, because of the following reasons:
    /// * the unions are dependent on language conventions, which could be changed at some point
    /// and the bits links between the data types could change with the change of those convetions.
    /// * some of the methods here are not endian agnostic. Read the summaries and the comments before
    /// using these unions.
    /// </summary>
    public static class Unions
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct Int32ByteUnion
        {
            [FieldOffset(0)]
            public byte Byte0;
            [FieldOffset(1)]
            public byte Byte1;
            [FieldOffset(2)]
            public byte Byte2;
            [FieldOffset(3)]
            public byte Byte3;

            [FieldOffset(0)]
            public int Int;
        }

        [StructLayout(LayoutKind.Explicit)]
        private ref struct UIntFloat
        {
            [FieldOffset(0)]
            public uint UInt;
            [FieldOffset(0)]
            public float Float;
        }

        [StructLayout(LayoutKind.Explicit)]
        private ref struct UIntNUInt
        {
            [FieldOffset(0)]
            public uint UInt;
            [FieldOffset(0)]
            public nuint NUInt;
        }

        [StructLayout(LayoutKind.Explicit)]
        private ref struct IntFloat
        {
            [FieldOffset(0)]
            public int Int;
            [FieldOffset(0)]
            public float Float;
        }

        [StructLayout(LayoutKind.Explicit)]
        private ref struct UIntUlong
        {
            [FieldOffset(0)]
            public ulong ULong;

            [FieldOffset(0)]
            public uint UIntLow;

            [FieldOffset(4)]
            public uint UIntHigh;
        }

        [StructLayout(LayoutKind.Explicit)]
        private ref struct ULongDouble
        {
            [FieldOffset(0)]
            public ulong ULong;
            [FieldOffset(0)]
            public double Double;
        }

        #region UInt-NUInt Union

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntLittleEndian(nuint x)
        {
            UIntNUInt uIntNUInt = default;
            // Respect the endianness of the machine.
            uIntNUInt.NUInt = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            return uIntNUInt.UInt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntBigEndian(nuint x)
        {
            UIntNUInt uIntNUInt = default;
            // Respect the endianness of the machine.
            uIntNUInt.NUInt = BitConverter.IsLittleEndian ? ReverseBits(x) : x;
            return uIntNUInt.UInt;
        }
        #endregion

        #region UInt-Ulong Union
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongLittleEndian(uint uintLow, uint uintHigh)
        {
            UIntUlong uIntUlong = default;
            uIntUlong.UIntLow = uintLow;
            uIntUlong.UIntHigh = uintHigh;
            return BitConverter.IsLittleEndian ? uIntUlong.ULong : ReverseBits(uIntUlong.ULong);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongBigEndian(uint uintLow, uint uintHigh)
        {
            UIntUlong uIntUlong = default;
            uIntUlong.UIntLow = uintLow;
            uIntUlong.UIntHigh = uintHigh;
            return BitConverter.IsLittleEndian ? ReverseBits(uIntUlong.ULong) : uIntUlong.ULong;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUIntLittleEndian(ulong x, out uint uintLow, out uint uintHigh)
        {
            UIntUlong uIntUlong = default;

            // Respect the endianness of the machine.
            uIntUlong.ULong = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            uintLow = uIntUlong.UIntLow;
            uintHigh = uIntUlong.UIntHigh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUIntBigEndian(ulong x, out uint uintLow, out uint uintHigh)
        {
            UIntUlong uIntUlong = default;

            // Respect the endianness of the machine.
            uIntUlong.ULong = BitConverter.IsLittleEndian ? ReverseBits(x) : x;
            uintLow = uIntUlong.UIntLow;
            uintHigh = uIntUlong.UIntHigh;
        }
        #endregion

        #region ULong-Double Union
        /// <summary>
        /// Returns double corresponding to the bit ordering of ulong.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDoubleLittleEndian(ulong x)
        {
            ULongDouble uLongDouble = default;
            // Respect the endianness of the machine.
            uLongDouble.ULong = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            return uLongDouble.Double;
        }

        /// <summary>
        /// Returns double corresponding to the the bit ordering of ulong.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDoubleBigEndian(ulong x)
        {
            ULongDouble uLongDouble = default;
            // Respect the endianness of the machine.
            uLongDouble.ULong = BitConverter.IsLittleEndian ? ReverseBits(x) : x;
            return uLongDouble.Double;
        }

        /// <summary>
        /// Returns ulong corresponding to the the bit ordering double.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongLittleEndian(double x)
        {
            ULongDouble uLongDouble = default;
            uLongDouble.Double = x;
            // Respect the endianness of the machine.
            return BitConverter.IsLittleEndian ? uLongDouble.ULong : ReverseBits(uLongDouble.ULong);
        }

        /// <summary>
        /// Returns ulong corresponding to the the bit ordering double.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongBigEndian(double x)
        {
            ULongDouble uLongDouble = default;
            uLongDouble.Double = x;
            // Respect the endianness of the machine.
            return BitConverter.IsLittleEndian ? ReverseBits(uLongDouble.ULong) : uLongDouble.ULong;
        }
        #endregion

        #region UInt-Float Union
        /// <summary>
        /// Returns float corresponding to the the bit ordering of uint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloatLittleEndian(uint x)
        {
            UIntFloat uIntFloat = default;
            // Respect the endianness of the machine.
            uIntFloat.UInt = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            return uIntFloat.Float;
        }

        /// <summary>
        /// Returns float corresponding to the the bit ordering of uint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloatBigEndian(uint x)
        {
            UIntFloat uIntFloat = default;
            // Respect the endianness of the machine.
            uIntFloat.UInt = BitConverter.IsLittleEndian ? ReverseBits(x) : x;
            return uIntFloat.Float;
        }

        /// <summary>
        /// Returns uint corresponding to the the bit ordering float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntLittleEndian(float x)
        {
            UIntFloat uIntFloat = default;
            uIntFloat.Float = x;
            // Respect the endianness of the machine.
            return BitConverter.IsLittleEndian ? uIntFloat.UInt : ReverseBits(uIntFloat.UInt);
        }

        /// <summary>
        /// Returns uint corresponding to the the bit ordering float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntBigEndian(float x)
        {
            UIntFloat uIntFloat = default;
            uIntFloat.Float = x;
            // Respect the endianness of the machine.
            return BitConverter.IsLittleEndian ? ReverseBits(uIntFloat.UInt) : uIntFloat.UInt;
        }
        #endregion

        #region Int-Float Union
        /// <summary>
        /// Avoid using this union for persistance data, because is not endian agnostic.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloatLittleEndian(int x)
        {
            IntFloat intFloat = default;
            intFloat.Int = x;
            return intFloat.Float;
        }

        /// <summary>
        /// Avoid using this union for persistance data, because is not endian agnostic.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetInt(float x)
        {
            IntFloat intFloat = default;
            intFloat.Float = x;
            return intFloat.Int;
        }
        #endregion
    }
}
