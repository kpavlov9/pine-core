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

        /**
         * For getting the most of the union values we want to respect the endianness of the machine, because
         * we might want to use ulong for some bit manipulations where we have some certain
         * assumption about endianness and then to switch to float preserving the order of the bits regrardless
         * of the machine endianness.
         **/

        #region UInt-NUInt Union

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUInt(nuint x)
        {
            UIntNUInt uIntNUInt = default;
            // Respect the endianness of the machine.
            uIntNUInt.NUInt = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            return uIntNUInt.UInt;
        }
        #endregion

        #region UInt-Ulong Union
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULong(uint uintLow, uint uintHigh)
        {
            UIntUlong uIntUlong = default;
            uIntUlong.UIntLow = uintLow;
            uIntUlong.UIntHigh = uintHigh;
            return BitConverter.IsLittleEndian ? uIntUlong.ULong : ReverseBits(uIntUlong.ULong);
        }

        public static void GetUInt(ulong x, out uint uintLow, out uint uintHigh)
        {
            UIntUlong uIntUlong = default;

            // Respect the endianness of the machine.
            uIntUlong.ULong = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            uintLow = uIntUlong.UIntLow;
            uintHigh = uIntUlong.UIntHigh;
        }
        #endregion

        #region ULong-Double Union
        /// <summary>
        /// Returns double corresponding to the input of little endian bit ordered ulong.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDouble(ulong x)
        {
            ULongDouble uLongDouble = default;
            // Respect the endianness of the machine.
            uLongDouble.ULong = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            return uLongDouble.Double;
        }

        /// <summary>
        /// Returns little endian bit ordered ulong corresponding to the input double.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULong(double x)
        {
            ULongDouble uLongDouble = default;
            uLongDouble.Double = x;
            // Respect the endianness of the machine.
            return BitConverter.IsLittleEndian ? uLongDouble.ULong : ReverseBits(uLongDouble.ULong);
        }
        #endregion

        #region UInt-Float Union
        /// <summary>
        /// Returns float corresponding to the input of little endian bit ordered uint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloat(uint x)
        {
            UIntFloat uIntFloat = default;
            // Respect the endianness of the machine.
            uIntFloat.UInt = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            return uIntFloat.Float;
        }

        /// <summary>
        /// Returns little endian bit ordered uint corresponding to the input float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUInt(float x)
        {
            UIntFloat uIntFloat = default;
            uIntFloat.Float = x;
            // Respect the endianness of the machine.
            return BitConverter.IsLittleEndian ? uIntFloat.UInt : ReverseBits(uIntFloat.UInt);
        }
        #endregion

        #region Int-Float Union
        /**
         * Since int is not used for bit operation we don't respect the endianness for the int-float union.
         * We mainly use it in some cases where we just need unique link between int and float without
         * requiring preserved bit order.
         **/

        /// <summary>
        /// Avoid using this union for persistance data, because is not endian agnostic.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloat(int x)
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
