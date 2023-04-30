using static KGIntelligence.PineCore.Helpers.Utilities.BitOps;

namespace PinusTests.units.PinusCore
{
    public class BitOpsTests
    {
        [Theory]
        [InlineData(0xffff0000, 20, 4, 0xffffffff00000000UL, 52, 4 + 16)]
        public void masked_pop_count(
            uint uintValue,
            uint uintOffset,
            uint uintPopCount,
            ulong ulongValue,
            ulong ulongOffset,
            ulong ulongPopCount)
        {

            var resultedUintPopCount = MaskedPopCount(uintValue, uintOffset);
            var resultedULongPopCount = MaskedPopCount(ulongValue, ulongOffset);
            Assert.True(uintPopCount == resultedUintPopCount);
            Assert.True(ulongPopCount == resultedULongPopCount);
        }

        [Theory]
        [InlineData(
            (byte)0xf0,
            (byte)0x0f,
            (ushort)0xff00,
            (ushort)0x00ff,
            0xffff0000,
            0x0000ffff,
            0xffffffff00000000UL,
            0x00000000ffffffffUL)]
        [InlineData(
            (byte)0x0f,
            (byte)0xf0,
            (ushort)0x00ff,
            (ushort)0xff00,
            0x0000ffff,
            0xffff0000,
            0x00000000ffffffffUL,
            0xffffffff00000000UL)]
        public void reverse_bits(
            byte byteValue,
            byte reversedByteBits,
            ushort ushortValue,
            ushort reversedUShortBits,
            uint uintValue,
            uint reversedUIntBits,
            ulong ulongValue,
            ulong reversedULongBits)
        {
            var resultedByteReversedBits = ReverseBits(byteValue);
            var resultedUShortReversedBits = ReverseBits(ushortValue);
            var resultedUIntReversedBits = ReverseBits(uintValue);
            var resultedULongReversedBits = ReverseBits(ulongValue);
            Assert.True(reversedByteBits == resultedByteReversedBits);
            Assert.True(reversedUShortBits == resultedUShortReversedBits);
            Assert.True(reversedUIntBits == resultedUIntReversedBits);
            Assert.True(reversedULongBits == resultedULongReversedBits);
        }

        [Theory]
        [InlineData(
            0xffff0000,
            17,
            1,
            0xffffffff00000000UL,
            33,
            1)]
        public void rank_of_reversed_set_bits(
            uint uintValue,
            int uIntPosition,
            int expectedUIntRank,
            ulong ulongValue,
            int uLongPosition,
            int expectedULongRank)
        {
            var resultedUIntReversedBits = RankOfReversed(
                value: uintValue,
                bitPositionCutoff: uIntPosition,
                blockSize: 32);

            var resultedULongReversedBits = RankOfReversed(
                value: ulongValue,
                bitPositionCutoff: uLongPosition,
                blockSize: 64);

            Assert.True(expectedUIntRank == resultedUIntReversedBits);
            Assert.True(expectedULongRank == resultedULongReversedBits);
        }


        [Theory]
        [InlineData(
            0xffff0000,
            17,
            16,
            0xffffffff00000000UL,
            33,
            32)]
        public void rank_of_reversed_unset_bits(
            uint uintValue,
            int uIntPosition,
            int expectedUIntRank,
            ulong ulongValue,
            int uLongPosition,
            int expectedULongRank)
        {

            var resultedUIntReversedBits = RankOfReversed(
                value: ~uintValue,
                bitPositionCutoff: uIntPosition,
                blockSize: 32);

            var resultedULongReversedBits = RankOfReversed(
                value: ~ulongValue,
                bitPositionCutoff: uLongPosition,
                blockSize: 64);

            Assert.True(expectedUIntRank == resultedUIntReversedBits);
            Assert.True(expectedULongRank == resultedULongReversedBits);
        }

        [Theory]
        [InlineData(
            0x0000ffff,
            21,
            5,
            0x00000000ffffffffUL,
            37,
            5)]
        public void rank_set_bits(
            uint uintValue,
            int uIntPosition,
            int expectedUIntRank,
            ulong ulongValue,
            int uLongPosition,
            int expectedULongRank)
        {

            var resultedUIntReversedBits = Rank(
                value: uintValue,
                bitPositionCutoff: uIntPosition,
                blockSize: 32);

            var resultedULongReversedBits = Rank(
                value: ulongValue,
                bitPositionCutoff: uLongPosition,
                blockSize: 64);

            Assert.True(expectedUIntRank == resultedUIntReversedBits);
            Assert.True(expectedULongRank == resultedULongReversedBits);
        }

        [Theory]
        [InlineData(
            0x0000ffff,
            17,
            16,
            0x00000000ffffffffUL,
            33,
            32)]
        public void rank_unset_bits(
            uint uintValue,
            int uIntPosition,
            int expectedUIntRank,
            ulong ulongValue,
            int uLongPosition,
            int expectedULongRank)
        {

            var resultedUIntReversedBits = Rank(
                value: ~uintValue,
                bitPositionCutoff: uIntPosition,
                blockSize: 32);

            var resultedULongReversedBits = Rank(
                value: ~ulongValue,
                bitPositionCutoff: uLongPosition,
                blockSize: 64);

            Assert.True(expectedUIntRank == resultedUIntReversedBits);
            Assert.True(expectedULongRank == resultedULongReversedBits);
        }

        [Theory]
        [InlineData(
            0xffff0000,
            15,
            15,
            0xffffffff00000000UL,
            31,
            31)]
        // If the index is greater than the maximum possible count of 0 or 1 bits,
        // we return the last index in the bit sequence.
        [InlineData(
            0xffff0000,
            16, // Greater than 15.
            31, // Last index.
            0xffffffff00000000UL,
            32, // Greater than 31.
            63)]// Last index.
        [InlineData(
            0xf0f0f0f0,
            5,
            9,
            0xf0f0f0f0f0f0f0f0UL,
            21,
            41)]
        public void select_set_bits(
            uint uintValue,
            int uIntPosition,
            int expectedUIntSelect,
            ulong ulongValue,
            int uLongPosition,
            int expectedULongSelect)
        {
            var resultedUIntBits = Select(
                value: uintValue,
                bitCountCutoff: uIntPosition);

            var resultedULongBits = Select(
                value: ulongValue,
                bitCountCutoff: uLongPosition);

            Assert.True(expectedUIntSelect == resultedUIntBits);
            Assert.True(expectedULongSelect == resultedULongBits);
        }

        [Theory]
        [InlineData(
            0xffff0000,
            15,
            31,
            0xffffffff00000000UL,
            31,
            63)]
        // If the index is greater than the maximum possible count of 0 or 1 bits,
        // we return the last index in the bit sequence.
        [InlineData(
            0xffff0000,
            16, // Greater than 15.
            31, // Last index.
            0xffffffff00000000UL,
            32, // Greater than 31.
            63)]// Last index.
        [InlineData(
            0xf0f0f0f0,
            5,
            13,
            0xf0f0f0f0f0f0f0f0UL,
            21,
            45)]
        [InlineData(
            0xffff0000,
            11,
            27,
            0xffffffff00000000UL,
            31,
            63)]
        public void select_unset_bits(
            uint uintValue,
            int uIntPosition,
            int expectedUIntSelect,
            ulong ulongValue,
            int uLongPosition,
            int expectedULongSelect)
        {
            var resultedUIntBits = Select(
                value: ~uintValue,
                bitCountCutoff: uIntPosition);

            var resultedULongBits = Select(
                value: ~ulongValue,
                bitCountCutoff: uLongPosition);

            Assert.True(expectedUIntSelect == resultedUIntBits);
            Assert.True(expectedULongSelect == resultedULongBits);
        }

        [Theory]
        [InlineData(
            0xffff0000,
            15,
            31,
            0xffffffff00000000UL,
            31,
            63)]
        [InlineData(
            0x0000ffff,
            16,
            15,
            0xffffffff00000000UL,
            31,
            63)]
        [InlineData(
            0xf0f0f0f0,
            15,
            31,
            0xf0f0f0f0f0f0f0f0UL,
            31,
            63)]
        public void select_of_reversed_set_bits(
            uint uintValue,
            int uIntPosition,
            int expectedUIntSelect,
            ulong ulongValue,
            int uLongPosition,
            int expectedULongSelect)
        {
            var resultedUIntReversedBits = SelectOfReversed(
                value: uintValue,
                bitCountCutoff: uIntPosition);

            var resultedULongReversedBits = SelectOfReversed(
                value: ulongValue,
                bitCountCutoff: uLongPosition);

            Assert.True(expectedUIntSelect == resultedUIntReversedBits);
            Assert.True(expectedULongSelect == resultedULongReversedBits);
        }


        [Theory]
        [InlineData(
            0xffff0000,
            16,
            15,
            0xffffffff00000000,
            32,
            31)]
        public void select_of_reversed_unset_bits(
            uint uintValue,
            int uIntPosition,
            int expectedUIntSelection,
            ulong ulongValue,
            int uLongPosition,
            int expectedULongSelection)
        {
            var resultedUIntReversedBits = SelectOfReversed(
                value: ~uintValue,
                bitCountCutoff: uIntPosition);

            var resultedULongReversedBits = SelectOfReversed(
                value: ~ulongValue,
                bitCountCutoff: uLongPosition);

            Assert.True(expectedUIntSelection == resultedUIntReversedBits);
            Assert.True(expectedULongSelection == resultedULongReversedBits);
        }
    }
}
