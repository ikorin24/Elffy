#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.Markup;

public static class StringConverter<T>
{
    private static Func<string, T>? _converter;

    public static void SetConverter(Func<string, T> converter) => _converter = converter;

    public static T Convert(string value)
    {
        if(typeof(T) == typeof(sbyte)) {
            var v = sbyte.Parse(value);
            return Unsafe.As<sbyte, T>(ref v);
        }
        if(typeof(T) == typeof(byte)) {
            var v = byte.Parse(value);
            return Unsafe.As<byte, T>(ref v);
        }
        if(typeof(T) == typeof(short)) {
            var v = short.Parse(value);
            return Unsafe.As<short, T>(ref v);
        }
        if(typeof(T) == typeof(ushort)) {
            var v = ushort.Parse(value);
            return Unsafe.As<ushort, T>(ref v);
        }
        if(typeof(T) == typeof(int)) {
            var v = int.Parse(value);
            return Unsafe.As<int, T>(ref v);
        }
        if(typeof(T) == typeof(uint)) {
            var v = uint.Parse(value);
            return Unsafe.As<uint, T>(ref v);
        }
        if(typeof(T) == typeof(long)) {
            var v = long.Parse(value);
            return Unsafe.As<long, T>(ref v);
        }
        if(typeof(T) == typeof(ulong)) {
            var v = ulong.Parse(value);
            return Unsafe.As<ulong, T>(ref v);
        }
        if(typeof(T) == typeof(float)) {
            var v = float.Parse(value);
            return Unsafe.As<float, T>(ref v);
        }
        if(typeof(T) == typeof(double)) {
            var v = double.Parse(value);
            return Unsafe.As<double, T>(ref v);
        }
        if(typeof(T) == typeof(nint)) {
            var v = nint.Parse(value);
            return Unsafe.As<nint, T>(ref v);
        }
        if(typeof(T) == typeof(nuint)) {
            var v = nuint.Parse(value);
            return Unsafe.As<nuint, T>(ref v);
        }
        if(typeof(T) == typeof(decimal)) {
            var v = decimal.Parse(value);
            return Unsafe.As<decimal, T>(ref v);
        }
        if(typeof(T) == typeof(string)) {
            return Unsafe.As<string, T>(ref value);
        }

        var converter = _converter;
        if(converter == null) { ThrowNoConverter(); }
        return converter.Invoke(value);
    }

    [DoesNotReturn]
    private static void ThrowNoConverter() => throw new InvalidOperationException($"Cannot convert string into '{typeof(T).FullName}'.");
}
