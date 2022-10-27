#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

public readonly ref struct RefOrNull<T>
{
    private readonly ref T _value;

    public static RefOrNull<T> NullRef => default;

    public bool IsNullRef
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(ref _value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefOrNull(ref T value) => _value = ref value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetReference() => ref _value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(T value)
    {
        _value = value;
    }

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
    public Ref<T> AsNotNull() => new Ref<T>(ref _value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefReadOnlyOrNull<T> AsReadOnly() => new RefReadOnlyOrNull<T>(in _value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator RefReadOnlyOrNull<T>(RefOrNull<T> r) => r.AsReadOnly();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Ref<T>(RefOrNull<T> r) => r.AsNotNull();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator RefReadOnly<T>(RefOrNull<T> r) => r.AsNotNull();
}
