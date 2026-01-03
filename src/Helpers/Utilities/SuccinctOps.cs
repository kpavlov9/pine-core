using System.Runtime.CompilerServices;
using System;

using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using System.Numerics;
using System.Runtime.InteropServices;

namespace KGIntelligence.PineCore.Helpers.Utilities
{
    internal static class SuccinctOps
    {
        internal const int BlockRate = 8;
        internal static readonly int SmallBlockSize = NativeBitCount;
        internal static readonly int LargeBlockSize = SmallBlockSize * BlockRate;
        internal static readonly uint BytesCountInValue = (uint)NativeBitCount / BitCountInByte;
        
        // RRR-specific constants
        internal static readonly byte BlockSize = NativeBitCountMinusOne; // 63 for 64-bit, 31 for 32-bit
        internal const nuint SuperBlockFactor = 32;
        internal static readonly nuint SuperBlockSize = BlockSize * SuperBlockFactor;
        internal static readonly byte BitsPerClass = (byte)(Is32BitSystem ? 5 : 6);

        private static readonly int ClassCountsStride = BlockSize + 2;
        internal static readonly nuint[] ClassCountsFlat;
        internal static readonly uint[] ClassBitOffsets;
        internal static readonly uint MaxBitsPerOffset;

        private const int InverseOffsetTableMaxSize = 65536;
        
        internal static readonly nuint[]?[] InverseOffsetTables;

        internal static readonly bool[] HasInverseOffsetTable;

        static SuccinctOps()
        {
            var blockSize = BlockSize;
            var stride = ClassCountsStride;
            
            // Initialize flattened ClassCounts (Pascal's triangle)
            ClassCountsFlat = new nuint[(blockSize + 1) * stride];
            
            for (int n = 0; n <= blockSize; n++)
            {
                ClassCountsFlat[n * stride + 0] = 1;
                
                for (int k = 1; k <= n; k++)
                {
                    // C(n,k) = C(n-1,k-1) + C(n-1,k)
                    ClassCountsFlat[n * stride + k] = 
                        GetClassCountFlat(n - 1, k - 1) + GetClassCountFlat(n - 1, k);
                }
                
                // Sentinel value for k > n
                ClassCountsFlat[n * stride + n + 1] = 0;
            }
            
            // Initialize ClassBitOffsets
            ClassBitOffsets = new uint[blockSize + 1];
            uint maxBits = 0;
            
            for (int n = 0; n <= blockSize; n++)
            {
                var elementsInClass = GetClassCountFlat(blockSize, n);
                var bits = (uint)System.Math.Ceiling(System.Math.Log2(elementsInClass + 1));
                ClassBitOffsets[n] = bits;
                maxBits = System.Math.Max(maxBits, bits);
            }
            MaxBitsPerOffset = maxBits;
            
            // Initialize InverseOffset lookup tables
            InverseOffsetTables = new nuint[blockSize + 1][];
            HasInverseOffsetTable = new bool[blockSize + 1];
            
            for (int @class = 0; @class <= blockSize; @class++)
            {
                var tableSize = GetClassCountFlat(blockSize, @class);
                
                if (tableSize <= InverseOffsetTableMaxSize && tableSize > 0)
                {
                    InverseOffsetTables[@class] = new nuint[(int)tableSize];
                    HasInverseOffsetTable[@class] = true;
                    BuildInverseOffsetTable(@class);
                }
            }
        }

    
        /// <summary>
        /// Get binomial coefficient C(n,k) from flattened array.
        /// Single memory access instead of two with jagged array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nuint GetClassCountFlat(int n, int k)
        {
            if (k < 0 || k > n) return 0;
            return ClassCountsFlat[n * ClassCountsStride + k];
        }

        /// <summary>
        /// Compute inverse offset using the original algorithm.
        /// Used for classes where table would be too large.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nuint ComputeInverseOffset(nuint offset, uint @class, int blockSize)
        {
            if (@class == 0)
                return 0;
            
            nuint block = 0;
            int i = blockSize - 1;
            
            while (i >= 0 && @class > 0)
            {
                nuint classCount = GetClassCountFlat(i, (int)@class);
                if (offset >= classCount)
                {
                    block |= NUIntOne << i;
                    offset -= classCount;
                    @class--;
                }
                i--;
            }
            
            return block;
        }

        /// <summary>
        /// Build the inverse offset table for a specific class.
        /// Uses combinatorial unranking algorithm.
        /// </summary>
        private static void BuildInverseOffsetTable(int @class)
        {
            var table = InverseOffsetTables[@class]!;
            var blockSize = BlockSize;
            
            for (int offset = 0; offset < table.Length; offset++)
            {
                table[offset] = ComputeInverseOffset((nuint)offset, (uint)@class, blockSize);
            }
        }

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
        internal static nuint GetMask(int rSmall) =>
            NUIntOne << rSmall;

        internal static void ValidatePosition(nuint position, nuint size)
        {
            if (position >= size)
            {
                throw new IndexOutOfRangeException(
                    $"The argument {nameof(position)} exceeds the sequence length {size}");
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

