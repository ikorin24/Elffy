#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy;

[DebuggerTypeProxy(typeof(Ref<>.DebuggerProxy))]
[DebuggerDisplay("{DebuggerView,nq}")]
public readonly ref struct Ref<T>
{
    private readonly ref T _value;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerView => Unsafe.IsNullRef(ref _value) ? "null" : $"{{ref {typeof(T).Name}}}";

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Ref() => throw new NotSupportedException("Don't use default constructor.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ref(ref T value)
    {
        if(Unsafe.IsNullRef(ref value)) {
            throw new ArgumentException("Reference is null");
        }
        _value = ref value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Ref(ref T value, bool noCheck)
    {
        // 'noCheck' is dummy arg
        Debug.Assert(noCheck);
        _value = ref value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ref<T> CreateUnsafe(ref T value) => new Ref<T>(ref value, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetReference() => ref _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSameRef(in T value) => Unsafe.AreSame(ref _value, ref Unsafe.AsRef(in value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(T value)
    {
        _value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Derefer() => _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefReadOnly<T> AsReadOnly() => RefReadOnly<T>.CreateUnsafe(in _value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefOrNull<T> AsNullable() => new RefOrNull<T>(ref _value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefReadOnlyOrNull<T> AsNullableReadOnly() => new RefReadOnlyOrNull<T>(in _value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ref<TTo> UnsafeAs<TTo>() => new Ref<TTo>(ref Unsafe.As<T, TTo>(ref _value), true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => new Span<T>(ref _value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsReadOnlySpan() => new ReadOnlySpan<T>(in _value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RefReadOnly<T>(Ref<T> r) => r.AsReadOnly();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RefOrNull<T>(Ref<T> r) => r.AsNullable();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RefReadOnlyOrNull<T>(Ref<T> r) => r.AsNullableReadOnly();

#pragma warning disable 0809
    [Obsolete("Not supported", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) => throw new NotSupportedException();

    [Obsolete("Not supported", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => throw new NotSupportedException();
#pragma warning restore 0809

    public static bool operator ==(Ref<T> left, Ref<T> right) =>
        Unsafe.AreSame(ref left._value, ref right._value);

    public static bool operator !=(Ref<T> left, Ref<T> right) => !(left == right);

    private sealed class DebuggerProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private T? _value;

        public T? Value => _value;

        public DebuggerProxy(Ref<T> r)
        {
            // Don't use Derefer()
            if(Unsafe.IsNullRef(ref r._value)) {
                _value = default;
            }
            else {
                _value = r._value;
            }
        }
    }
}
