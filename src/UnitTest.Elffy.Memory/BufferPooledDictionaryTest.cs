#nullable enable
using System;
using Xunit;
using Elffy.Effective;

namespace UnitTest
{
    public class BufferPooledDictionaryTest
    {
        [Fact]
        public void BasicTest()
        {
            using var dic = new BufferPooledDictionary<int, long>();

            const int count = 1000;

            for(int i = 0; i < count; i++) {
                dic.Add(i, i);
            }

            Assert.Equal(count, dic.Count);
            foreach(var key in dic.Keys) {
                Assert.Equal((long)key, dic[key]);
                Assert.True(dic.TryGetValue(key, out var value) && value == (long)key);
                Assert.True(dic.ContainsKey(key));
                Assert.True(dic.ContainsValue(dic[key]));
            }

            foreach(var value in dic.Values) {
                Assert.True(dic.ContainsValue(value));
            }

            foreach(var (key, value) in dic) {
                Assert.Equal((long)key, value);
                Assert.Equal(dic[key], value);
                Assert.True(dic.TryGetValue(key, out var v) && v == value);
                Assert.True(dic.ContainsKey(key));
                Assert.True(dic.ContainsValue(dic[key]));
            }

            Assert.True(dic.Values.Count == dic.Count);
            Assert.True(dic.Keys.Count == dic.Count);

            Assert.False(dic.TryAdd(10, 10));
            Assert.False(dic.TryAdd(0, 0));
            Assert.False(dic.TryAdd(999, 999));

            Assert.True(dic.TryAdd(10000, 10000));
            dic.Clear();

            Assert.True(dic.Count == 0);
            foreach(var _ in dic) { throw new Exception(); }
            foreach(var _ in dic.Keys) { throw new Exception(); }
            foreach(var _ in dic.Values) { throw new Exception(); }
        }
    }
}
