#nullable enable
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf;

[DebuggerDisplay("{ToString()}")]
[JsonConverter(typeof(U8StringConverter))]
internal readonly struct U8String
{
    private static readonly Encoding _encoding = Encoding.UTF8;

    private readonly byte[]? _bytes;

    public static U8String Empty => default;

    public bool IsEmpty => _bytes == null || _bytes.Length == 0;

    public int ByteLength => _bytes?.Length ?? 0;

    public ref byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var bytes = _bytes;
            if(bytes == null) {
                ThrowOutOfRange();
                [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
            }
            return ref bytes[index];
        }
    }

    internal U8String(byte[] bytes)
    {
        _bytes = bytes;
    }

    public U8String(ReadOnlySpan<byte> bytes)
    {
        _bytes = bytes.ToArray();
    }

    public static U8String Create<TState>(int length, TState state, SpanAction<byte, TState> action)
    {
        if(length < 0) {
            throw new ArgumentOutOfRangeException(nameof(length));
        }
        ArgumentNullException.ThrowIfNull(action);
        if(length == 0) {
            return Empty;
        }
        var bytes = new byte[length];
        action.Invoke(bytes, state);
        return new U8String(bytes);
    }

    public override string ToString() => _encoding.GetString(_bytes.AsSpan());

    public int GetCharCount() => _encoding.GetCharCount(_bytes.AsSpan());

    public ReadOnlySpan<byte> AsSpan() => _bytes.AsSpan();
    public ReadOnlySpan<byte> AsSpan(int start) => _bytes.AsSpan(start);
    public ReadOnlySpan<byte> AsSpan(int start, int length) => _bytes.AsSpan(start, length);
}

internal sealed class U8StringConverter : JsonConverter<U8String>
{
    public override U8String Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.String) { throw new JsonException(); }
        return Unescape(reader.ValueSpan);
    }

    public override void Write(Utf8JsonWriter writer, U8String value, JsonSerializerOptions options) => throw new NotSupportedException();

    private static U8String Unescape(ReadOnlySpan<byte> input)
    {
        var hasEscaped = false;
        U8StringBuf unescaped = default;
        var current = 0;

        int i = 0;
        while(true) {
            // regex
            // \\[Uu]([0-9A-Fa-f]{4})
            //
            // (ex) \u0abc

            if(i >= input.Length - 5) {
                break;
            }

            if(input[i] is (byte)'\\'
                && input[i + 1] is (byte)'u' or (byte)'U'
                && TryHexAsciiToInt(input[i + 5], out var h0)
                && TryHexAsciiToInt(input[i + 4], out var h1)
                && TryHexAsciiToInt(input[i + 3], out var h2)
                && TryHexAsciiToInt(input[i + 2], out var h3)
                ) {
                var rune = new Rune((h3 << 12) + (h2 << 8) + (h1 << 4) + h0);
                if(i != 0) {
                    var s = input[current..i];
                    unescaped.Append(s);
                }
                unescaped.Append(rune);
                i += 6;
                current = i;
                hasEscaped = true;
            }
            else {
                i++;
            }
        }

        if(hasEscaped) {
            var result = unescaped.ToU8String();
            return result;
        }
        else {
            var result = new U8String(input);
            return result;
        }
    }

    private static bool TryHexAsciiToInt(byte hexAscii, out int num)
    {
        if(hexAscii is (>= (byte)'0' and <= (byte)'9')) {
            num = hexAscii - (byte)'0';
            return true;
        }
        else if(hexAscii is (>= (byte)'a' and <= (byte)'f')) {
            num = hexAscii - (byte)'a' + 10;
            return true;
        }
        else if(hexAscii is (>= (byte)'A' and <= (byte)'F')) {
            num = hexAscii - (byte)'A' + 10;
            return true;
        }
        num = default;
        return false;
    }

    private ref struct U8StringBuf
    {
        private int _length;
        private Span<byte> _buf;

        public int ByteLength => _length;

        public void Append(ReadOnlySpan<byte> bytes)
        {
            var margin = _buf.Length - _length;
            if(margin - bytes.Length < 0) {
                var newCapacity = Math.Max(_buf.Length + bytes.Length, Math.Max(_buf.Length * 2, 4));
                var newBuf = new byte[newCapacity];
                _buf.CopyTo(newBuf);
                _buf = newBuf;
            }
            bytes.CopyTo(_buf.Slice(_length));
            _length += bytes.Length;
        }

        public void Append(Rune rune)
        {
            Span<byte> tmp = stackalloc byte[4];
            var encoded = tmp.Slice(0, rune.EncodeToUtf8(tmp));

            var margin = _buf.Length - _length;
            if(margin - encoded.Length < 0) {
                var newCapacity = Math.Max(_buf.Length + encoded.Length, Math.Max(_buf.Length * 2, 4));
                var newBuf = new byte[newCapacity];
                _buf.CopyTo(newBuf);
                _buf = newBuf;
            }
            encoded.CopyTo(_buf.Slice(_length));
            _length += encoded.Length;
        }

        public U8String ToU8String() => new U8String(_buf.Slice(0, _length));
    }
}
