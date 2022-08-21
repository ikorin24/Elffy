#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf;

internal struct Accessor
{
    public int? bufferView = null;
    public int byteOffset = 0;
    public AccessorComponentType componentType = default;  // must
    public bool normalized = false;
    public int count = default;  // must
    public AccessorType type = default;   // must
    public float[]? max = null;
    public float[]? min = null;
    public AccessorSparse? sparse = null;
    public U8String? name = null;

    public Accessor()
    {
    }
}

internal struct AccessorSparse
{
    public int count = default;  // must
    public AccessorSparseIndices indices = new();   // must
    public AccessorSparseValues values = new();     // must

    public AccessorSparse()
    {
    }
}

internal struct AccessorSparseValues
{
    public int bufferView = default; // must
    public int byteOffset = 0;

    public AccessorSparseValues()
    {
    }
}

internal struct AccessorSparseIndices
{
    public int bufferView = default; // must
    public int byteOffset = 0;
    public AccessorSparseIndicesComponentType componentType = default;  // must
    public AccessorSparseIndices()
    {
    }
}

[JsonConverter(typeof(AccessorSparseIndicesComponentTypeConverter))]
internal enum AccessorSparseIndicesComponentType
{
    UnsignedByte,
    UnsignedShort,
    UnsignedInt,
}

internal sealed class AccessorSparseIndicesComponentTypeConverter : JsonConverter<AccessorSparseIndicesComponentType>
{
    private const int UNSIGNED_BYTE = 5121;
    private const int UNSIGNED_SHORT = 5123;
    private const int UNSIGNED_INT = 5125;

    public override AccessorSparseIndicesComponentType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.Number) { throw new JsonException(); }
        var value = reader.GetInt32();
        return value switch
        {
            UNSIGNED_BYTE => AccessorSparseIndicesComponentType.UnsignedByte,
            UNSIGNED_SHORT => AccessorSparseIndicesComponentType.UnsignedShort,
            UNSIGNED_INT => AccessorSparseIndicesComponentType.UnsignedInt,
            _ => throw new JsonException(),
        };
    }

    public override void Write(Utf8JsonWriter writer, AccessorSparseIndicesComponentType value, JsonSerializerOptions options) => throw new NotSupportedException();
}


[JsonConverter(typeof(AccessorTypeConverter))]
internal enum AccessorType
{
    Scalar,
    Vec2,
    Vec3,
    Vec4,
    Mat2,
    Mat3,
    Mat4,
}

internal sealed class AccessorTypeConverter : JsonConverter<AccessorType>
{
    private static ReadOnlySpan<byte> SCALAR => new byte[6] { (byte)'S', (byte)'C', (byte)'A', (byte)'L', (byte)'A', (byte)'R' };
    private static ReadOnlySpan<byte> VEC2 => new byte[4] { (byte)'V', (byte)'E', (byte)'C', (byte)'2' };
    private static ReadOnlySpan<byte> VEC3 => new byte[4] { (byte)'V', (byte)'E', (byte)'C', (byte)'3' };
    private static ReadOnlySpan<byte> VEC4 => new byte[4] { (byte)'V', (byte)'E', (byte)'C', (byte)'4' };
    private static ReadOnlySpan<byte> MAT2 => new byte[4] { (byte)'M', (byte)'A', (byte)'T', (byte)'2' };
    private static ReadOnlySpan<byte> MAT3 => new byte[4] { (byte)'M', (byte)'A', (byte)'T', (byte)'3' };
    private static ReadOnlySpan<byte> MAT4 => new byte[4] { (byte)'M', (byte)'A', (byte)'T', (byte)'4' };

    public override AccessorType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.String) {
            throw new JsonException();
        }
        var value = reader.ValueSpan;
        if(value.SequenceEqual(SCALAR)) { return AccessorType.Scalar; }
        else if(value.SequenceEqual(VEC2)) { return AccessorType.Vec2; }
        else if(value.SequenceEqual(VEC3)) { return AccessorType.Vec3; }
        else if(value.SequenceEqual(VEC4)) { return AccessorType.Vec4; }
        else if(value.SequenceEqual(MAT2)) { return AccessorType.Mat2; }
        else if(value.SequenceEqual(MAT3)) { return AccessorType.Mat3; }
        else if(value.SequenceEqual(MAT4)) { return AccessorType.Mat4; }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, AccessorType value, JsonSerializerOptions options) => throw new NotSupportedException();
}

[JsonConverter(typeof(AccessorComponentTypeConverter))]
internal enum AccessorComponentType
{
    Byte,
    UnsignedByte,
    Short,
    UnsignedShort,
    UnsignedInt,
    Float,
}

internal sealed class AccessorComponentTypeConverter : JsonConverter<AccessorComponentType>
{
    private const int BYTE = 5120;
    private const int UNSIGNED_BYTE = 5121;
    private const int SHORT = 5122;
    private const int UNSIGNED_SHORT = 5123;
    private const int UNSIGNED_INT = 5125;
    private const int FLOAT = 5126;

    public override AccessorComponentType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.Number) { throw new JsonException(); }
        var value = reader.GetInt32();
        return value switch
        {
            BYTE => AccessorComponentType.Byte,
            UNSIGNED_BYTE => AccessorComponentType.UnsignedByte,
            SHORT => AccessorComponentType.Short,
            UNSIGNED_SHORT => AccessorComponentType.UnsignedShort,
            UNSIGNED_INT => AccessorComponentType.UnsignedInt,
            FLOAT => AccessorComponentType.Float,
            _ => throw new JsonException()
        };
    }

    public override void Write(Utf8JsonWriter writer, AccessorComponentType value, JsonSerializerOptions options) => throw new NotSupportedException();
}
