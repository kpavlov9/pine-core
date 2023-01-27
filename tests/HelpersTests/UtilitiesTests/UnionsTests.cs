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
            Assert.Equal(GetUInt(GetFloat(@uint)), @uint);
        }

        [Theory]
        [InlineData(ulong.MinValue)]
        [InlineData(ulong.MaxValue)]
        [InlineData(0)]
        [InlineData(1)]
        public void double_ulong(ulong @ulong)
        {
            Assert.Equal(GetULong(GetDouble(@ulong)), @ulong);
        }
    }
}
