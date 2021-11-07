#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elffy.Effective;
using Xunit;

namespace UnitTest
{
    public class RefTypeRentMemoryTest
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(8)]
        [InlineData(20)]
        [InlineData(200)]
        [InlineData(3000)]
        [InlineData(1024 * 1024)]
        public void Rent(int length)
        {
            using(var memory = new RefTypeRentMemory<string?>(length)) {
                var span = memory.AsSpan();
                Assert.Equal(length, span.Length);
                foreach(var item in span) {
                    Assert.Null(item);
                }
            }
        }

        [Theory]
        [InlineData(1024 * 1024 * 1024)]
        public void LargeRent(int length)
        {
            using(var memory = new RefTypeRentMemory<string?>(length)) {
                var span = memory.AsSpan();
                Assert.Equal(length, span.Length);
            }
        }

        [Fact]
        public void RentEmpty()
        {
            using(var memory = new RefTypeRentMemory<string?>(0)) {
                Assert.True(memory.AsSpan().IsEmpty);
                Assert.Equal(0, memory.Length);
            }
        }

        [Fact]
        public void RentMulti()
        {
            const int Count = 100;
            using var list = new Disposables<RefTypeRentMemory<string?>>();
            for(int i = 0; i < Count; i++) {
                var mem = new RefTypeRentMemory<string?>(1000);
                list.Add(mem);
            }
            for(int i = 0; i < Count; i++) {
                Assert.Equal(1000, list[i].Length);
                Assert.Equal(1000, list[i].AsSpan().Length);
            }
        }

        [Fact]
        public void RentMultiParallel()
        {
            Parallel.For(0, 100, i =>
            {
                using var mem = new RefTypeRentMemory<string?>(1000);
                Assert.Equal(1000, mem.Length);
                Assert.Equal(1000, mem.AsSpan().Length);
            });
        }

        [Fact]
        public void Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new RefTypeRentMemory<string?>(-1);
            });

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new RefTypeRentMemory<string?>(-10);
            });

            Assert.Throws<OutOfMemoryException>(() =>
            {
                new RefTypeRentMemory<string?>(int.MaxValue);
            });
        }

        private sealed class Disposables<TDisposable> : IDisposable where TDisposable : IDisposable
        {
            private List<TDisposable> _list = new();

            public TDisposable this[int index]
            {
                get => _list[index];
            }

            public void Add(TDisposable disposable)
            {
                _list.Add(disposable);
            }

            public void Dispose()
            {
                foreach(var item in _list) {
                    item.Dispose();
                }
                _list.Clear();
            }
        }
    }
}
