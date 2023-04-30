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
        public static uint GetUInt(nuint x)
        {
            UIntNUInt union = default;
            union.NUInt = x;
            return union.UInt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntLittleEndian(nuint x)
            => BitConverter.IsLittleEndian
            ? GetUInt(x)
            : ReverseBits(GetUInt(x));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntBigEndian(nuint x)
            => BitConverter.IsLittleEndian
            ? ReverseBits(GetUInt(x))
            : GetUInt(x);
        #endregion

        #region UInt-ULong Union
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULong(uint low, uint high)
        {
            UIntULong union = default;
            union.UIntLow = low;
            union.UIntHigh = high;
            return union.ULong;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongLittleEndian(uint low, uint high)
            => BitConverter.IsLittleEndian
            ? GetULong(low, high)
            : ReverseBits(GetULong(low, high));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongBigEndian(uint low, uint high)
            => BitConverter.IsLittleEndian
            ? ReverseBits(GetULong(low, high))
            : GetULong(low, high);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUInt(
            ulong x,
            out uint low,
            out uint high)
        {
            UIntULong union = default;
            union.ULong = x;
            low = union.UIntLow;
            high = union.UIntHigh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUIntLittleEndian(
            ulong x,
            out uint low,
            out uint high)
        {
            if (BitConverter.IsLittleEndian)
            {
                GetUInt(x, out low, out high);
            }
            else
            {
                GetUInt(ReverseBits(x), out low, out high);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUIntBigEndian(ulong x, out uint low, out uint high)
        {
            if (BitConverter.IsLittleEndian)
            {
                GetUInt(ReverseBits(x), out low, out high);
            }
            else
            {
                GetUInt(x, out low, out high);
            }
        }
        #endregion

        #region UShort-UInt Union
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUInt(ushort low, ushort high)
        {
            UShortUInt union = default;
            union.UShortLow = low;
            union.UShortHigh = high;
            return union.UInt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntLittleEndian(ushort low, ushort high)
            => BitConverter.IsLittleEndian
            ? GetUInt(low, high)
            : ReverseBits(GetUInt(low, high));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntBigEndian(ushort low, ushort high)
            => BitConverter.IsLittleEndian
            ? ReverseBits(GetUInt(low, high))
            : GetUInt(low, high);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUShort(
            uint x,
            out ushort low,
            out ushort high)
        {
            UShortUInt union = default;
            union.UInt = x;
            low = union.UShortLow;
            high = union.UShortHigh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUShortLittleEndian(
            uint x,
            out ushort low,
            out ushort high)
        {
            if (BitConverter.IsLittleEndian)
            {
                GetUShort(x, out low, out high);
            }
            else
            {
                GetUShort(ReverseBits(x), out low, out high);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetUShortBigEndian(
            uint x,
            out ushort low,
            out ushort high)
        {
            if (BitConverter.IsLittleEndian)
            {
                GetUShort(ReverseBits(x), out low, out high);
            }
            else
            {
                GetUShort(x, out low, out high);
            }
        }
        #endregion

        #region ULong-Double Union
        /// <summary>
        /// Returns double corresponding to bit ordering of ulong.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDouble(ulong x)
        {
            ULongDouble union = default;
            union.ULong = x;
            return union.Double;
        }

        /// <summary>
        /// Returns double corresponding to bit ordering of ulong.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDoubleLittleEndian(ulong x)
            => GetDouble(BitConverter.IsLittleEndian
                ? x
                : ReverseBits(x));

        /// <summary>
        /// Returns double corresponding to the bit ordering of ulong.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDoubleBigEndian(ulong x)
            => GetDouble(BitConverter.IsLittleEndian
                ? ReverseBits(x)
                : x);

        /// <summary>
        /// Returns ulong corresponding to the bit ordering of double.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULong(double x)
        {
            ULongDouble union = default;
            union.Double = x;
            return union.ULong;
        }

        /// <summary>
        /// Returns ulong corresponding to the bit ordering of double.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongLittleEndian(double x)
            => BitConverter.IsLittleEndian
            ? GetULong(x)
            : ReverseBits(GetULong(x));

        /// <summary>
        /// Returns ulong corresponding to the bit ordering of double.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetULongBigEndian(double x)
        {
            ULongDouble union = default;
            union.Double = x;
            // Respect the endianness of the machine.
            return BitConverter.IsLittleEndian
                ? ReverseBits(union.ULong)
                : union.ULong;
        }
        #endregion

        #region UInt-Float Union
        /// <summary>
        /// Returns float corresponding to the bit ordering of uint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloat(uint x)
        {
            UIntFloat union = default;
            union.UInt = x;
            return union.Float;
        }

        /// <summary>
        /// Returns float corresponding to the bit ordering of uint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloatLittleEndian(uint x)
            => GetFloat(BitConverter.IsLittleEndian
                ? x
                : ReverseBits(x));

        /// <summary>
        /// Returns float corresponding to the bit ordering of uint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFloatBigEndian(uint x)
            => GetFloat(BitConverter.IsLittleEndian
                ? ReverseBits(x)
                : x);

        /// <summary>
        /// Returns uint corresponding to the bit ordering of float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUInt(float x)
        {
            UIntFloat union = default;
            union.Float = x;
            return union.UInt;
        }

        /// <summary>
        /// Returns uint corresponding to the bit ordering of float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntLittleEndian(float x)
            => BitConverter.IsLittleEndian
            ? GetUInt(x)
            : ReverseBits(GetUInt(x));

        /// <summary>
        /// Returns uint corresponding to the bit ordering of float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetUIntBigEndian(float x)
            => BitConverter.IsLittleEndian
            ? ReverseBits(GetUInt(x))
            : GetUInt(x);
        #endregion

        #region
        /// <summary>
        /// Returns half corresponding to the bit ordering of ushort.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half GetHalf(ushort x)
        {
            UShortHalf union = default;
            union.UShort = x;
            return union.Half;
        }

        /// <summary>
        /// Returns half corresponding to the bit ordering of ushort.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half GetHalfLittleEndian(ushort x)
            => BitConverter.IsLittleEndian
            ? GetHalf(x)
            : GetHalf(ReverseBits(x));

        /// <summary>
        /// Returns half corresponding to the bit ordering of ushort.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half GetHalfBigEndian(ushort x)
            => BitConverter.IsLittleEndian
            ? GetHalf(ReverseBits(x))
            : GetHalf(x);

        /// <summary>
        /// Returns ushort corresponding to the bit ordering of half.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetUShort(Half x)
        {
            UShortHalf union = default;
            union.Half = x;
            return union.UShort;
        }

        /// <summary>
        /// Returns ushort corresponding to bit ordering of half.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetUShortLittleEndian(Half x)
            => BitConverter.IsLittleEndian
            ? GetUShort(x)
            : ReverseBits(GetUShort(x));

        /// <summary>
        /// Returns ushort corresponding to bit ordering of half.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetUShortBigEndian(Half x)
            => BitConverter.IsLittleEndian
            ? ReverseBits(GetUShort(x))
            : GetUShort(x);
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
