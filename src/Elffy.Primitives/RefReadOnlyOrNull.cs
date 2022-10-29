#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

[DebuggerTypeProxy(typeof(RefReadOnlyOrNull<>.DebuggerProxy))]
[DebuggerDisplay("{DebuggerView,nq}")]
public readonly ref struct RefReadOnlyOrNull<T>
{
    private readonly ref readonly T _value;

    public static RefReadOnlyOrNull<T> NullRef => default;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerView => IsNullRef ? "null" : $"{{ref {typeof(T).Name}}}";

    public bool IsNullRef
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(ref Unsafe.AsRef(in _value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefReadOnlyOrNull(in T value)
    {
        _value = ref value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetReference() => ref _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSameRef(in T value) => Unsafe.AreSame(ref Unsafe.AsRef(in _value), ref Unsafe.AsRef(in value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T DereferOrThrow()
    {
        if(IsNullRef) {
            throw new InvalidOperationException("Reference is null");
        }
        return _value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryDerefer([MaybeNullWhen(false)] out T dereferenced)
    {
        if(IsNullRef) {
            dereferenced = default;
            return false;
        }
        dereferenced = _value;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: MaybeNull]
    public T DereferOrDefault() => IsNullRef ? default : _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T DereferOrDefault(T defaultValue) => IsNullRef ? defaultValue : _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefReadOnly<T> AsNotNull() => new RefReadOnly<T>(in _value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator RefReadOnly<T>(RefReadOnlyOrNull<T> r) => r.AsNotNull();

#pragma warning disable 0809
    [Obsolete("Not supported", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) => throw new NotSupportedException();

    [Obsolete("Not supported", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => throw new NotSupportedException();
#pragma warning restore 0809

    public static bool operator ==(RefReadOnlyOrNull<T> left, RefReadOnlyOrNull<T> right) =>
        Unsafe.AreSame(ref Unsafe.AsRef(in left._value), ref Unsafe.AsRef(in right._value));

    public static bool operator !=(RefReadOnlyOrNull<T> left, RefReadOnlyOrNull<T> right) => !(left == right);

    private sealed class DebuggerProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private T? _value;

        public T? Value => _value;

        public DebuggerProxy(RefReadOnlyOrNull<T> r)
        {
            r.TryDerefer(out _value);
        }
    }
}
