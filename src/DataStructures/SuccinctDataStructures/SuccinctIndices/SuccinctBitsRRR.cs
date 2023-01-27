using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using KGIntelligence.PineCore.Helpers.Utilities;
using static KGIntelligence.PineCore.Helpers.Utilities.NativeBitOps;
using KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.BitIndices;
using static KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices.SuccinctBitsRRRBuilder;

namespace KGIntelligence.PineCore.DataStructures.SuccinctDataStructures.SuccinctIndices
{

    /// <summary>
    /// Compressed bit sequence offering rank and select queries with the need of decopression.
    /// The paper which ispires the implementation is
    /// "Succinct Indexable Dictionaries with Applications to Encoding k-ary Trees and Multisets"
    /// by Rajeev Raman, V.Raman and S.Srinivasa Rao: http://arxiv.org/pdf/0705.0552.pdf
    ///
    /// The abbreviation 'RRR' comes from the first letters of the familiy names of 3 authors of the paper.
    /// </summary>
    public readonly struct SuccinctBitsRRR : IBitIndices<SuccinctBitsRRR>, ISuccinctIndices
    {
        private readonly Bits _classValues;
        private readonly Bits _offsetValues;

        /// <summary>
        /// The length of the sequence.
        /// </summary>
        private readonly nuint _size;

        /// <summary>
        /// Total count of set bits.
        /// </summary>
        private readonly nuint _setBitsCount;

        private readonly QuasiSuccinctBits _rankSamples;
        private readonly QuasiSuccinctBits _offsetPositionSamples;

        public nuint Size => _size;

        public SuccinctBitsRRR(
            nuint size,
            nuint setBitsCount,
            in QuasiSuccinctBits rankSamples,
            in QuasiSuccinctBits offsetPositionSamples,
            in Bits classValues,
            in Bits offsetValues)
        {
            _rankSamples = rankSamples;
            _offsetPositionSamples = offsetPositionSamples;
            _size = size;
            _setBitsCount = setBitsCount;
            _classValues = classValues;
            _offsetValues = offsetValues;
        }

        public bool GetBit(nuint bitPosition)
        {
            nuint blockPosition = bitPosition / BlockSize;
            var @class = ClassOfBlock(blockPosition, _classValues);
            if (@class == 0)
            {
                // All bit of the block is 0.
                return false;
            }
            if (@class == BlockSize)
            {
                // All bit of the block is 1.
                return true;
            }
            var block = FetchBlock(
                blockPosition,
                @class,
                _offsetPositionSamples,
                _offsetValues,
                _classValues);

            int offsetInBlock = (int)(bitPosition % BlockSize);
            nuint mask = NUIntOne << BlockSize - 1 - offsetInBlock;
            return (block & mask) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nuint FetchBlock(
            nuint blockPosition,
            uint @class,
            in QuasiSuccinctBits offsetPosSamples,
            in Bits offsetValues,
            in Bits classValues) => InverseOffsetOf(
                OffsetOfBlock(
                    blockPosition,
                    @class,
                    offsetPosSamples,
                    offsetValues,
                    classValues), @class);

        private nuint FetchBlock(nuint blockPosition)
        {
            uint @class = ClassOfBlock(blockPosition, _classValues);
            return InverseOffsetOf(
                OffsetOfBlock(
                    blockPosition,
                    @class,
                    _offsetPositionSamples,
                    _offsetValues,
                    _classValues),
                @class);
        }

        internal static nuint OffsetOfBlock(
            nuint blockPosition,
            uint @class,
            in QuasiSuccinctBits offsetPosSamples,
            in Bits offsetValues,
            in Bits classValues)
        {
            var offsetCount = (int)ClassBitOffsets[@class];

            var superBlockIndex = blockPosition / SuperBlockFactor;
            var offsetPosition = offsetPosSamples.Get(superBlockIndex);
            var j = superBlockIndex * SuperBlockFactor;

            while (j < blockPosition)
            {
                var jthClass = ClassOfBlock(j, classValues);
                offsetPosition += ClassBitOffsets[jthClass];
                j++;
            }

            return offsetValues.FetchBits(offsetPosition, offsetCount);
        }

        public static nuint InverseOffsetOf(nuint offset, uint @class)
        {
            if (@class == 0)
            {
                return 0;
            }

            var classCounts = ClassCounts;
            nuint block = 0;
            nuint classCount;
            var i = BlockSize - 1;

            do
            {
                classCount = classCounts[i][@class];
                if (offset >= classCount)
                {
                    block |= NUIntOne << i;
                    offset -= classCount;
                    --@class;
                    if (@class <= 0)
                    {
                        return block;
                    }
                }
                --i;
            } while (i >= 0);

            return block;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ClassOfBlock(nuint blockPosition, in Bits classValues)
        {
            var bitsPerClass = BitsPerClass;
            return unchecked((uint)classValues.FetchBits(
                blockPosition * bitsPerClass,
                bitsPerClass));
        }

        public nuint GetBitsCount(bool forSetBits)
        {
            return forSetBits ? _setBitsCount : _size - _setBitsCount;
        }

        public nuint Rank(nuint bitPositionCutoff, bool forSetBits)
        {
            return forSetBits ? RankOnes(bitPositionCutoff) : bitPositionCutoff - RankOnes(bitPositionCutoff);
        }

        private nuint RankOnes(nuint bitIndex)
        {
            var blockSize = BlockSize;
            nuint blockIndex = bitIndex / blockSize;
            var superBlockFactor = SuperBlockFactor;
            var superBlockSize = SuperBlockSize;

            nuint superBlockIndex = blockIndex / superBlockFactor;
            nuint rank = _rankSamples.Get(superBlockIndex);
            if (superBlockIndex + 1 < _rankSamples.Size)
            {
                // FetchBits the next sample.
                nuint rankNext = _rankSamples.Get(superBlockIndex + 1);
                nuint delta = rankNext - rank;
                if (delta == 0)
                {
                    // All the bits in the superBlockIndex-th super-block are 0.
                    return rank;
                }
                else if (delta == superBlockSize)
                {
                    // All the bits in the super-block are 1.
                    nuint superBlockHead = superBlockIndex * superBlockSize;
                    return rank + (bitIndex - superBlockHead);
                }
            }

            nuint j = superBlockIndex * superBlockFactor;

            while (j < blockIndex)
            {
                rank += ClassOfBlock(j, _classValues);
                j++;
            }

            var block = FetchBlock(blockIndex);
            int posInTheBlock = (int)(bitIndex % blockSize);
            // Least significant bits are set.
            nuint mask = (NUIntOne << blockSize - posInTheBlock) - 1;
            // Most significant bits are set.
            mask = ~mask;
            return rank + PopCount(block & mask);
        }

        public nuint Select(nuint bitCountCutoff, bool forSetBits)
        {
            return forSetBits ? SelectSetBits(bitCountCutoff) : SelectNotSetBits(bitCountCutoff);
        }

        private nuint SelectSetBits(nuint bitCountCutoff)
        {
            if (bitCountCutoff >= GetBitsCount(true))
            {
                throw new ArgumentOutOfRangeException(
                    @$"The argument '{nameof(bitCountCutoff)}' with value '{bitCountCutoff}'
 exceeds the total number of 1s = {GetBitsCount(true)}");
            }

            var rankSamples = _rankSamples;
            nuint left = 0;
            nuint right = rankSamples.Size - 1;

            while (left < right)
            {
                nuint pivot = left + right >> 1;
                nuint rankAtThePivot = rankSamples.Get(pivot);
                if (bitCountCutoff < rankAtThePivot)
                {
                    right = pivot;
                }
                else
                {
                    left = pivot + 1;
                }
            }
            right--;

            nuint blockPosition = right * SuperBlockFactor;
            var blockSize = BlockSize;

            var rank = rankSamples.Get(right);
            nuint delta = rankSamples.Get(right + 1) - rank;

            if (delta == SuperBlockSize)
            {
                // The bits in the left super-block are 1.
                return blockPosition * blockSize + (bitCountCutoff - rank);
            }

            nuint blocksCount = (_size + blockSize - 1) / blockSize;
            nuint intialI = bitCountCutoff;
            bitCountCutoff -= rank;

            while (true)
            {
#if DEBUG
                Debug.Assert(
                    blockPosition < blocksCount,
                    @$"The block position exceedes '{blocksCount}' for the bits count limit of '{intialI}'.");
#endif

                uint @class = ClassOfBlock(blockPosition, _classValues);

                if (bitCountCutoff < @class) { break; }
                blockPosition++;
                bitCountCutoff -= @class;
            }
            nuint block = FetchBlock(blockPosition);
            nuint bitAlignedBlock = block << NativeBitCount - blockSize;
            return blockPosition * blockSize + NativeBitOps.Select(
                bitAlignedBlock,
                (int)bitCountCutoff, true);
        }

        private nuint SelectNotSetBits(nuint bitPosition)
        {
            if (bitPosition >= GetBitsCount(false))
            {
                throw new ArgumentOutOfRangeException(
                    @$"The zero bit position of '{bitPosition}'
 exceeds the total count of zeros - '{GetBitsCount(false)}'.");
            }

            var superBlockSize = SuperBlockSize;
            var blockSize = BlockSize;

            nuint left = 0;
            nuint right = _rankSamples.Size - 1;
            while (left < right)
            {
                nuint pivot = left + right >> 1;
                nuint rankAtThePivot =
                    pivot * superBlockSize - _rankSamples.Get(pivot);

                if (bitPosition < rankAtThePivot)
                {
                    right = pivot;
                }
                else
                {
                    left = pivot + 1;
                }
            }
            right--;

            nuint j = right * SuperBlockFactor;
            nuint rank = _rankSamples.Get(right);
            nuint delta = _rankSamples.Get(right + 1) - rank;

            if (delta == 0)
            {
                // every bit in the left-th super-block is 0
                return j * blockSize + (bitPosition - (rank + 1));
            }

            bitPosition -= right * superBlockSize - rank;

            while (true)
            {
                uint c = ClassOfBlock(j, _classValues);
                uint r = blockSize - c;
                if (bitPosition < r) { break; }
                j++;
                bitPosition -= r;
            }

            nuint block = FetchBlock(j);
            nuint bitAlignedBlock = block << NativeBitCount - blockSize;
            return j * blockSize + NativeBitOps.Select(bitAlignedBlock, (int)bitPosition, false);
        }

        public static SuccinctBitsRRR Read(BinaryReader reader)
        {
            var size = reader.ReadNUInt();
            var setBitsCount = reader.ReadNUInt();

            var classValues = Bits.Read(reader);
            var offsetValues = Bits.Read(reader);
            var rankSamples = QuasiSuccinctBits.Read(reader);
            var offsetPosSamples = QuasiSuccinctBits.Read(reader);

            return new SuccinctBitsRRR(
                size: size,
                setBitsCount: setBitsCount,
                rankSamples: rankSamples,
                offsetPositionSamples: offsetPosSamples,
                classValues: classValues,
                offsetValues: offsetValues);
        }

        public static SuccinctBitsRRR Read(string filename)
        {
            var reader = new BinaryReader(
                new FileStream(filename, FileMode.Open, FileAccess.Read));
            return Read(reader);
        }

        public void Write(BinaryWriter writer)
        {
            writer.WriteNUInt(_size);
            writer.WriteNUInt(_setBitsCount);

            _classValues.Write(writer);
            _offsetValues.Write(writer);
            _rankSamples.Write(writer);
            _offsetPositionSamples.Write(writer);
        }

        public void Write(string filename)
        {
            var writer = new BinaryWriter(
                new FileStream(filename, FileMode.Create, FileAccess.Write));
            Write(writer);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var bv = (SuccinctBitsRRR)obj;
            return _size == bv._size &&
                _setBitsCount == bv._setBitsCount &&
                _classValues.Equals(bv._classValues) &&
                _offsetValues.Equals(bv._offsetValues);
        }

        public override int GetHashCode()
        {
            return _classValues.GetHashCode() * _offsetValues.GetHashCode();
        }

        public static bool operator ==(SuccinctBitsRRR left, SuccinctBitsRRR right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SuccinctBitsRRR left, SuccinctBitsRRR right)
        {
            return !(left == right);
        }
    }
}
