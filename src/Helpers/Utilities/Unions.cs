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
        public struct Int32Byte
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
        private ref struct UShortHalf
        {
            [FieldOffset(0)]
            public ushort UShort;
            [FieldOffset(0)]
            public Half Half;
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
        private ref struct UShortUInt
        {
            [FieldOffset(0)]
            public uint UInt;

            [FieldOffset(0)]
            public ushort UShortLow;

            [FieldOffset(2)]
            public ushort UShortHigh;
        }

        [StructLayout(LayoutKind.Explicit)]
        private ref struct UIntULong
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
            UIntNUInt union = default;
            // Respect the endianness of the machine.
            union.NUInt = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            return union.UInt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntBigEndian(nuint x)
        {
            UIntNUInt union = default;
            // Respect the endianness of the machine.
            union.NUInt = BitConverter.IsLittleEndian ? ReverseBits(x) : x;
            return union.UInt;
        }
        #endregion

        #region UInt-ULong Union
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongLittleEndian(uint low, uint high)
        {
            UIntULong union = default;
            union.UIntLow = low;
            union.UIntHigh = high;
            return BitConverter.IsLittleEndian ? union.ULong : ReverseBits(union.ULong);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongBigEndian(uint low, uint high)
        {
            UIntULong union = default;
            union.UIntLow = low;
            union.UIntHigh = high;
            return BitConverter.IsLittleEndian ? ReverseBits(union.ULong) : union.ULong;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUIntLittleEndian(ulong x, out uint low, out uint high)
        {
            UIntULong union = default;

            // Respect the endianness of the machine.
            union.ULong = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            low = union.UIntLow;
            high = union.UIntHigh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUIntBigEndian(ulong x, out uint low, out uint high)
        {
            UIntULong uIntUlong = default;

            // Respect the endianness of the machine.
            uIntUlong.ULong = BitConverter.IsLittleEndian ? ReverseBits(x) : x;
            low = uIntUlong.UIntLow;
            high = uIntUlong.UIntHigh;
        }
        #endregion

        #region UShort-UInt Union
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntLittleEndian(ushort low, ushort high)
        {
            UShortUInt union = default;
            union.UShortLow = low;
            union.UShortHigh = high;
            return BitConverter.IsLittleEndian ? union.UInt : ReverseBits(union.UInt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntBigEndian(ushort low, ushort high)
        {
            UShortUInt union = default;
            union.UShortLow = low;
            union.UShortHigh = high;
            return BitConverter.IsLittleEndian ? ReverseBits(union.UInt) : union.UInt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUShortLittleEndian(
            uint x,
            out ushort low,
            out ushort high)
        {
            UShortUInt union = default;

            // Respect the endianness of the machine.
            union.UInt = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            low = union.UShortLow;
            high = union.UShortHigh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUShortBigEndian(
            uint x,
            out ushort low,
            out ushort high)
        {
            UShortUInt union = default;

            // Respect the endianness of the machine.
            union.UInt = BitConverter.IsLittleEndian ? ReverseBits(x) : x;
            low = union.UShortLow;
            high = union.UShortHigh;
        }
        #endregion

        #region ULong-Double Union
        /// <summary>
        /// Returns double corresponding to the bit ordering of ulong.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDoubleLittleEndian(ulong x)
        {
            ULongDouble union = default;
            // Respect the endianness of the machine.
            union.ULong = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            return union.Double;
        }

        /// <summary>
        /// Returns double corresponding to the the bit ordering of ulong.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDoubleBigEndian(ulong x)
        {
            ULongDouble union = default;
            // Respect the endianness of the machine.
            union.ULong = BitConverter.IsLittleEndian ? ReverseBits(x) : x;
            return union.Double;
        }

        /// <summary>
        /// Returns ulong corresponding to the the bit ordering double.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongLittleEndian(double x)
        {
            ULongDouble union = default;
            union.Double = x;
            // Respect the endianness of the machine.
            return BitConverter.IsLittleEndian ? union.ULong : ReverseBits(union.ULong);
        }

        /// <summary>
        /// Returns ulong corresponding to the the bit ordering double.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongBigEndian(double x)
        {
            ULongDouble union = default;
            union.Double = x;
            // Respect the endianness of the machine.
            return BitConverter.IsLittleEndian ? ReverseBits(union.ULong) : union.ULong;
        }
        #endregion

        #region UInt-Float Union
        /// <summary>
        /// Returns float corresponding to the the bit ordering of uint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloatLittleEndian(uint x)
        {
            UIntFloat union = default;
            // Respect the endianness of the machine.
            union.UInt = BitConverter.IsLittleEndian ? x : ReverseBits(x);
            return union.Float;
        }

        /// <summary>
        /// Returns float corresponding to the the bit ordering of uint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloatBigEndian(uint x)
        {
            UIntFloat union = default;
            // Respect the endianness of the machine.
            union.UInt = BitConverter.IsLittleEndian ? ReverseBits(x) : x;
            return union.Float;
        }

        /// <summary>
        /// Returns uint corresponding to the the bit ordering float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntLittleEndian(float x)
        {
            UIntFloat union = default;
            union.Float = x;
            // Respect the endianness of the machine.
            return BitConverter.IsLittleEndian ? union.UInt : ReverseBits(union.UInt);
        }

        /// <summary>
        /// Returns uint corresponding to the the bit ordering float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntBigEndian(float x)
        {
            UIntFloat union = default;
            union.Float = x;
            // Respect the endianness of the machine.
            return BitConverter.IsLittleEndian ? ReverseBits(union.UInt) : union.UInt;
        }
        #endregion

        #region

        /// <summary>
        /// Returns half corresponding to the the bit ordering of uint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half GetHalf(ushort x)
        {
            UShortHalf union = default;
            union.UShort = x;
            return union.Half;
        }

        /// <summary>
        /// Returns uint corresponding to the the bit ordering float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetUShort(Half x)
        {
            UShortHalf union = default;
            union.Half = x;
            return union.UShort;
        }
        #endregion

        #region Int-Float Union
        /// <summary>
        /// Avoid using this union for persistance data, because is not endian agnostic.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloat(int x)
        {
            IntFloat union = default;
            union.Int = x;
            return union.Float;
        }

        /// <summary>
        /// Avoid using this union for persistance data, because is not endian agnostic.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetInt(float x)
        {
            IntFloat union = default;
            union.Float = x;
            return union.Int;
        }
        #endregion
    }
}
