#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf;

[JsonConverter(typeof(Matrix4Converter))]
[DebuggerTypeProxy(typeof(Matrix4TypeProxy))]
internal unsafe struct Matrix4
{
    private fixed float _m[16];

    public ref float this[int index] => ref _m[index];

    public static Matrix4 Identity => new()
    {
        [0] = 1,
        [1] = 0,
        [2] = 0,
        [3] = 0,
        [4] = 0,
        [5] = 1,
        [6] = 0,
        [7] = 0,
        [8] = 0,
        [9] = 0,
        [10] = 1,
        [11] = 0,
        [12] = 0,
        [13] = 0,
        [14] = 0,
        [15] = 1,
    };

    public Span<float> AsSpan() => MemoryMarshal.CreateSpan(ref _m[0], 16);
}

internal sealed class Matrix4TypeProxy
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Matrix4 _matrix;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public float[] Values => _matrix.AsSpan().ToArray();

    public Matrix4TypeProxy(Matrix4 matrix)
    {
        _matrix = matrix;
    }
}

internal sealed class Matrix4Converter : JsonConverter<Matrix4>
{
    public override Matrix4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // expected [number, number, ..., number]

        if(reader.TokenType != JsonTokenType.StartArray) { throw new JsonException(); }
        reader.Read();
        if(reader.TokenType != JsonTokenType.Number) { throw new JsonException(); }
        Matrix4 value;
        value[0] = ReadSingle(ref reader);
        value[1] = ReadSingle(ref reader);
        value[2] = ReadSingle(ref reader);
        value[3] = ReadSingle(ref reader);
        value[4] = ReadSingle(ref reader);
        value[5] = ReadSingle(ref reader);
        value[6] = ReadSingle(ref reader);
        value[7] = ReadSingle(ref reader);
        value[8] = ReadSingle(ref reader);
        value[9] = ReadSingle(ref reader);
        value[10] = ReadSingle(ref reader);
        value[11] = ReadSingle(ref reader);
        value[12] = ReadSingle(ref reader);
        value[13] = ReadSingle(ref reader);
        value[14] = ReadSingle(ref reader);
        value[15] = ReadSingle(ref reader);

        if(reader.TokenType != JsonTokenType.EndArray) { throw new JsonException(); }
        return value;

        static float ReadSingle(ref Utf8JsonReader reader)
        {
            var f = reader.GetSingle();
            reader.Read();
            return f;
        }
    }

    public override void Write(Utf8JsonWriter writer, Matrix4 value, JsonSerializerOptions options) => throw new NotSupportedException();
}
