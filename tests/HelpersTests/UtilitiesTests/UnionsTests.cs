using static KGIntelligence.PineCore.Helpers.Utilities.Unions;

namespace PinusTests.units.PinusCore
{
    public class UnionsTests
    {
        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        public void float_int(int @int)
        {
            Assert.Equal(@int, GetInt(GetFloat(@int)));
        }

        [Theory]
        [InlineData(uint.MinValue)]
        [InlineData(uint.MaxValue)]
        [InlineData(0)]
        [InlineData(1)]
        public void float_uint(uint @uint)
        {
            Assert.Equal(GetUIntLittleEndian(GetFloatLittleEndian(@uint)), @uint);
            Assert.Equal(GetUIntBigEndian(GetFloatBigEndian(@uint)), @uint);
        }

        [Theory]
        [InlineData(ulong.MinValue)]
        [InlineData(ulong.MaxValue)]
        [InlineData(0)]
        [InlineData(1)]
        public void double_ulong(ulong @ulong)
        {
            Assert.Equal(GetULongLittleEndian(GetDoubleLittleEndian(@ulong)), @ulong);
            Assert.Equal(GetULongBigEndian(GetDoubleBigEndian(@ulong)), @ulong);
        }

        [Theory]
        [InlineData(ulong.MinValue)]
        [InlineData(ulong.MaxValue)]
        [InlineData(0)]
        [InlineData(1)]
        public void ulong_uint(ulong @ulong)
        {
            GetUIntLittleEndian(@ulong, out var low, out var high);
            Assert.Equal(GetULongLittleEndian(low: low, high: high), @ulong);

            GetUIntBigEndian(@ulong, out low, out high);
            Assert.Equal(GetULongBigEndian(low: low, high: high), @ulong);
        }
    }
}
