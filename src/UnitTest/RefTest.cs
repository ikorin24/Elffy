#nullable enable
using Elffy;
using Elffy.Effective.Unsafes;
using System.Runtime.CompilerServices;
using Xunit;

namespace UnitTest
{
    public class RefTest
    {
        [Fact]
        public void New()
        {
            int value = 10;

            {
                // Ref<T>
                var r = new Ref<int>(ref value);
                Assert.Equal(value, r.Derefer());
                Assert.True(Unsafe.AreSame(ref r.GetReference(), ref value));
            }
            {
                // RefReadOnly<T>
                var r = new RefReadOnly<int>(in value);
                Assert.Equal(value, r.Derefer());
                Assert.True(UnsafeEx.AreSame(in r.GetReference(), in value));
            }
            {
                // RefOrNull<T>
                var r = new RefOrNull<int>(ref value);
                Assert.Equal(value, r.DereferOrThrow());
                Assert.Equal(value, r.DereferOrDefault());
                Assert.Equal(value, r.DereferOrDefault(-1));
                Assert.True(Unsafe.AreSame(ref r.GetReference(), ref value));
            }
            {
                // RefReadOnlyOrNull<T>
                var r = new RefReadOnlyOrNull<int>(in value);
                Assert.Equal(value, r.DereferOrThrow());
                Assert.Equal(value, r.DereferOrDefault());
                Assert.Equal(value, r.DereferOrDefault(-1));
                Assert.True(UnsafeEx.AreSame(in r.GetReference(), in value));
            }
        }

#pragma warning disable 0183
        [Fact]
        public void CastFromRef()
        {
            int value = 10;

            // Ref<T> -> X
            {
                // Ref<T> -> RefReadOnly<T>
                var r = new Ref<int>(ref value);
                RefReadOnly<int> r1 = r;          // implicit
                var r2 = (RefReadOnly<int>)r;     // explicit
                var r3 = r.AsReadOnly();          // method

                Assert.True(r1 is RefReadOnly<int>);
                Assert.True(r2 is RefReadOnly<int>);
                Assert.True(r3 is RefReadOnly<int>);
                Assert.True(r1 == r2);
                Assert.True(r1 == r3);
                Assert.Equal(value, r1.Derefer());
            }
            {
                // Ref<T> -> RefOrNull<T>
                var r = new Ref<int>(ref value);
                RefOrNull<int> r1 = r;            // implicit
                var r2 = (RefOrNull<int>)r;       // explicit
                var r3 = r.AsNullable();          // method

                Assert.True(r1 is RefOrNull<int>);
                Assert.True(r2 is RefOrNull<int>);
                Assert.True(r3 is RefOrNull<int>);
                Assert.True(r1 == r2);
                Assert.True(r1 == r3);
                Assert.Equal(value, r1.DereferOrThrow());
            }
            {
                // Ref<T> -> RefReadOnlyOrNull<T>
                var r = new Ref<int>(ref value);
                RefReadOnlyOrNull<int> r1 = r;        // implicit
                var r2 = (RefReadOnlyOrNull<int>)r;   // explicit
                var r3 = r.AsNullableReadOnly();      // method

                Assert.True(r1 is RefReadOnlyOrNull<int>);
                Assert.True(r2 is RefReadOnlyOrNull<int>);
                Assert.True(r3 is RefReadOnlyOrNull<int>);
                Assert.True(r1 == r2);
                Assert.True(r1 == r3);
                Assert.Equal(value, r1.DereferOrThrow());
            }
        }

        [Fact]
        public void CastFromRefOrNull()
        {
            int value = 10;

            // RefOrNull<T> -> X
            {
                // RefOrNull<T> -> Ref<T>
                var r = new RefOrNull<int>(ref value);
                var r1 = (Ref<int>)r;             // explicit
                var r2 = r.AsNotNull();           // method

                Assert.True(r1 is Ref<int>);
                Assert.True(r2 is Ref<int>);
                Assert.True(r1 == r2);
                Assert.Equal(value, r1.Derefer());
            }
            {
                // RefOrNull<T> -> RefReadOnly<T>
                var r = new RefOrNull<int>(ref value);
                var r1 = (RefReadOnly<int>)r;       // explicit
                var r2 = r.AsNotNullReadOnly();     // method

                Assert.True(r1 is RefReadOnly<int>);
                Assert.True(r2 is RefReadOnly<int>);
                Assert.True(r1 == r2);
                Assert.Equal(value, r1.Derefer());
            }
            {
                // RefOrNull<T> -> RefReadOnlyOrNull<T>
                var r = new RefOrNull<int>(ref value);
                var r1 = (RefReadOnlyOrNull<int>)r;   // explicit
                var r2 = r.AsReadOnly();              // method

                Assert.True(r1 is RefReadOnlyOrNull<int>);
                Assert.True(r2 is RefReadOnlyOrNull<int>);
                Assert.True(r1 == r2);
                Assert.Equal(value, r1.DereferOrThrow());
            }
        }

        [Fact]
        public void CastFromRefReadOnly()
        {
            // TODO:
        }

        [Fact]
        public void CastFromRefReadOnlyOrNull()
        {
            // TODO:
        }
#pragma warning restore 0183
    }
}
