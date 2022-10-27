#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy;

public readonly ref struct RefReadOnly<T>
{
    private readonly ref readonly T _value;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public RefReadOnly() => throw new NotSupportedException("Don't use default constructor.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefReadOnly(in T value)
    {
        if(Unsafe.IsNullRef(ref Unsafe.AsRef(in value))) {
            throw new ArgumentException("Reference is null");
        }
        _value = ref value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private RefReadOnly(in T value, bool noCheck)
    {
        // 'noCheck' is dummy arg
        Debug.Assert(noCheck);
        _value = ref value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReference() => ref _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSameRef(in T value) => Unsafe.AreSame(ref Unsafe.AsRef(in _value), ref Unsafe.AsRef(in value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefReadOnly<T> CreateUnsafe(in T value) => new RefReadOnly<T>(in value, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Derefer() => _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefReadOnlyOrNull<T> AsNullable() => new RefReadOnlyOrNull<T>(in _value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RefReadOnlyOrNull<T>(RefReadOnly<T> r) => r.AsNullable();

#pragma warning disable 0809
    [Obsolete("Not supported", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) => throw new NotSupportedException();

    [Obsolete("Not supported", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => throw new NotSupportedException();
#pragma warning restore 0809

    public static bool operator ==(RefReadOnly<T> left, RefReadOnly<T> right) =>
        Unsafe.AreSame(ref Unsafe.AsRef(in left._value), ref Unsafe.AsRef(in right._value));

    public static bool operator !=(RefReadOnly<T> left, RefReadOnly<T> right) => !(left == right);
}
