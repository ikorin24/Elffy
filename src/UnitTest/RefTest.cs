#nullable enable
using Elffy;
using Elffy.Effective.Unsafes;
using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace UnitTest;

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
            Assert.True(r.IsSameRef(in value));
        }
        {
            // RefReadOnly<T>
            var r = new RefReadOnly<int>(in value);
            Assert.Equal(value, r.Derefer());
            Assert.True(UnsafeEx.AreSame(in r.GetReference(), in value));
            Assert.True(r.IsSameRef(in value));
        }
        {
            // RefOrNull<T>
            var r = new RefOrNull<int>(ref value);
            Assert.Equal(value, r.DereferOrThrow());
            Assert.Equal(value, r.DereferOrDefault());
            Assert.Equal(value, r.DereferOrDefault(-1));
            Assert.True(r.TryDerefer(out var dereferenced));
            Assert.Equal(value, dereferenced);
            Assert.True(Unsafe.AreSame(ref r.GetReference(), ref value));
            Assert.True(r.IsSameRef(in value));
        }
        {
            // RefReadOnlyOrNull<T>
            var r = new RefReadOnlyOrNull<int>(in value);
            Assert.Equal(value, r.DereferOrThrow());
            Assert.Equal(value, r.DereferOrDefault());
            Assert.Equal(value, r.DereferOrDefault(-1));
            Assert.True(r.TryDerefer(out var dereferenced));
            Assert.Equal(value, dereferenced);
            Assert.True(UnsafeEx.AreSame(in r.GetReference(), in value));
            Assert.True(r.IsSameRef(in value));
        }
    }

    [Fact]
    public void NullDereference()
    {
        {
            // Ref<T>
            Assert.Throws<ArgumentException>(() =>
            {
                new Ref<int>(ref Unsafe.NullRef<int>());
            });
        }
        {
            // RefReadOnly<T>
            Assert.Throws<ArgumentException>(() =>
            {
                new RefReadOnly<int>(in Unsafe.NullRef<int>());
            });
        }
        {
            // RefOrNull<T>
            Assert.True(RefOrNull<int>.NullRef.IsNullRef);
            Assert.False(RefOrNull<int>.NullRef.TryDerefer(out _));
            Assert.Throws<InvalidOperationException>(() =>
            {
                RefOrNull<int>.NullRef.DereferOrThrow();
            });
            Assert.Equal(default, RefOrNull<int>.NullRef.DereferOrDefault());
            Assert.Equal(-100, RefOrNull<int>.NullRef.DereferOrDefault(-100));
            Assert.True(RefOrNull<int>.NullRef.IsSameRef(in Unsafe.NullRef<int>()));
        }
        {
            // RefReadOnlyOrNull<T>
            Assert.True(RefReadOnlyOrNull<int>.NullRef.IsNullRef);
            Assert.False(RefReadOnlyOrNull<int>.NullRef.TryDerefer(out _));
            Assert.Throws<InvalidOperationException>(() =>
            {
                RefReadOnlyOrNull<int>.NullRef.DereferOrThrow();
            });
            Assert.Equal(default, RefReadOnlyOrNull<int>.NullRef.DereferOrDefault());
            Assert.Equal(-100, RefReadOnlyOrNull<int>.NullRef.DereferOrDefault(-100));
            Assert.True(RefReadOnlyOrNull<int>.NullRef.IsSameRef(in Unsafe.NullRef<int>()));
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
            Assert.Equal(value, r2.Derefer());
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
            Assert.Equal(value, r2.Derefer());
        }
        {
            // RefOrNull<T> -> RefReadOnlyOrNull<T>
            var r = new RefOrNull<int>(ref value);
            var r1 = (RefReadOnlyOrNull<int>)r;     // explicit
            RefReadOnlyOrNull<int> r2 = r;          // implicit
            var r3 = r.AsReadOnly();                // method

            Assert.True(r1 is RefReadOnlyOrNull<int>);
            Assert.True(r2 is RefReadOnlyOrNull<int>);
            Assert.True(r3 is RefReadOnlyOrNull<int>);
            Assert.True(r1 == r2);
            Assert.True(r1 == r3);
            Assert.Equal(value, r1.DereferOrThrow());
            Assert.Equal(value, r2.DereferOrThrow());
            Assert.Equal(value, r3.DereferOrThrow());
        }
    }

    [Fact]
    public void CastFromRefReadOnly()
    {
        int value = 10;

        {
            // RefReadOnly<T> -> RefReadOnlyOrNull<T>
            var r = new RefReadOnly<int>(in value);
            var r1 = (RefReadOnlyOrNull<int>)r;     // explicit
            RefReadOnlyOrNull<int> r2 = r;          // implicit
            var r3 = r.AsNullable();                // method

            Assert.True(r1 is RefReadOnlyOrNull<int>);
            Assert.True(r2 is RefReadOnlyOrNull<int>);
            Assert.True(r3 is RefReadOnlyOrNull<int>);
            Assert.True(r1 == r2);
            Assert.True(r1 == r3);
            Assert.Equal(value, r1.DereferOrThrow());
            Assert.Equal(value, r2.DereferOrThrow());
            Assert.Equal(value, r3.DereferOrThrow());
        }
    }

    [Fact]
    public void CastFromRefReadOnlyOrNull()
    {
        int value = 10;

        {
            // RefReadOnlyOrNull<T> -> RefReadOnly<T>
            var r = new RefReadOnlyOrNull<int>(in value);
            var r1 = (RefReadOnly<int>)r;       // explicit
            var r2 = r.AsNotNull();             // method

            Assert.True(r1 is RefReadOnly<int>);
            Assert.True(r2 is RefReadOnly<int>);
            Assert.True(r1 == r2);
            Assert.Equal(value, r1.Derefer());
            Assert.Equal(value, r2.Derefer());
        }
    }
#pragma warning restore 0183
}
