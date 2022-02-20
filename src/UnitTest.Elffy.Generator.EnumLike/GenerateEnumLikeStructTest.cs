#nullable enable
using Xunit;
using Elffy;

namespace UnitTest
{
    public class GenerateEnumLikeStructTest
    {
        [Fact]
        public void ToStringTest_Int32()
        {
            var nameValues = TestEnumInt32.AllNameValues().Span;
            foreach(var (name, value) in nameValues) {
                var toStringName = value.ToString();
                Assert.Equal(name, toStringName);
            }
        }

        [Fact]
        public void ToStringTest_UInt32()
        {
            var nameValues = TestEnumUInt32.AllNameValues().Span;
            foreach(var (name, value) in nameValues) {
                var toStringName = value.ToString();
                Assert.Equal(name, toStringName);
            }
        }

        [Fact]
        public void ToStringTest_Int16()
        {
            var nameValues = TestEnumInt16.AllNameValues().Span;
            foreach(var (name, value) in nameValues) {
                var toStringName = value.ToString();
                Assert.Equal(name, toStringName);
            }
        }

        [Fact]
        public void ToStringTest_UInt16()
        {
            var nameValues = TestEnumUInt16.AllNameValues().Span;
            foreach(var (name, value) in nameValues) {
                var toStringName = value.ToString();
                Assert.Equal(name, toStringName);
            }
        }

        [Fact]
        public void ToStringTest_Int8()
        {
            var nameValues = TestEnumInt8.AllNameValues().Span;
            foreach(var (name, value) in nameValues) {
                var toStringName = value.ToString();
                Assert.Equal(name, toStringName);
            }
        }

        [Fact]
        public void ToStringTest_UInt8()
        {
            var nameValues = TestEnumUInt8.AllNameValues().Span;
            foreach(var (name, value) in nameValues) {
                var toStringName = value.ToString();
                Assert.Equal(name, toStringName);
            }
        }

        [Fact]
        public void ToStringTest_Int64()
        {
            var nameValues = TestEnumInt64.AllNameValues().Span;
            foreach(var (name, value) in nameValues) {
                var toStringName = value.ToString();
                Assert.Equal(name, toStringName);
            }
        }

        [Fact]
        public void ToStringTest_UInt64()
        {
            var nameValues = TestEnumUInt64.AllNameValues().Span;
            foreach(var (name, value) in nameValues) {
                var toStringName = value.ToString();
                Assert.Equal(name, toStringName);
            }
        }
    }

    [GenerateEnumLikeStruct(typeof(int))]
    [EnumLikeValue("None", 0)]
    [EnumLikeValue("Value15", 15)]
    [EnumLikeValue("Value20", 20)]
    [EnumLikeValue("ValueMax", int.MaxValue)]
    [EnumLikeValue("ValueMin", int.MinValue)]
    [EnumLikeValue("ValueMinus30", -30)]
    [EnumLikeValue("ValueMinus20", -20)]
    internal partial struct TestEnumInt32
    {
    }

    [GenerateEnumLikeStruct(typeof(uint))]
    [EnumLikeValue("None", 0)]
    [EnumLikeValue("Value15", 15)]
    [EnumLikeValue("Value20", 20)]
    [EnumLikeValue("ValueMax", uint.MaxValue)]
    internal partial struct TestEnumUInt32
    {
    }

    [GenerateEnumLikeStruct(typeof(short))]
    [EnumLikeValue("None", 0)]
    [EnumLikeValue("Value15", 15)]
    [EnumLikeValue("Value20", 20)]
    [EnumLikeValue("ValueMax", short.MaxValue)]
    [EnumLikeValue("ValueMin", short.MinValue)]
    [EnumLikeValue("ValueMinus30", -30)]
    [EnumLikeValue("ValueMinus20", -20)]
    internal partial struct TestEnumInt16
    {
    }

    [GenerateEnumLikeStruct(typeof(ushort))]
    [EnumLikeValue("None", 0)]
    [EnumLikeValue("Value15", 15)]
    [EnumLikeValue("Value20", 20)]
    [EnumLikeValue("ValueMax", ushort.MaxValue)]
    internal partial struct TestEnumUInt16
    {
    }

    [GenerateEnumLikeStruct(typeof(sbyte))]
    [EnumLikeValue("None", 0)]
    [EnumLikeValue("Value15", 15)]
    [EnumLikeValue("Value20", 20)]
    [EnumLikeValue("ValueMax", sbyte.MaxValue)]
    [EnumLikeValue("ValueMin", sbyte.MinValue)]
    [EnumLikeValue("ValueMinus30", -30)]
    [EnumLikeValue("ValueMinus20", -20)]
    internal partial struct TestEnumInt8
    {
    }

    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue("None", 0)]
    [EnumLikeValue("Value15", 15)]
    [EnumLikeValue("Value20", 20)]
    [EnumLikeValue("ValueMax", byte.MaxValue)]
    internal partial struct TestEnumUInt8
    {
    }

    [GenerateEnumLikeStruct(typeof(long))]
    [EnumLikeValue("None", 0)]
    [EnumLikeValue("Value15", 15)]
    [EnumLikeValue("Value20", 20)]
    [EnumLikeValue("ValueMax", long.MaxValue)]
    [EnumLikeValue("ValueMin", long.MinValue)]
    [EnumLikeValue("ValueMinus30", -30)]
    [EnumLikeValue("ValueMinus20", -20)]
    internal partial struct TestEnumInt64
    {
    }

    [GenerateEnumLikeStruct(typeof(ulong))]
    [EnumLikeValue("None", 0)]
    [EnumLikeValue("Value15", 15)]
    [EnumLikeValue("Value20", 20)]
    [EnumLikeValue("ValueMax", ulong.MaxValue)]
    internal partial struct TestEnumUInt64
    {
    }
}
