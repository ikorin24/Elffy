#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf.Parsing;

internal struct Buffer
{
    public U8String? uri = null;
    public nuint byteLength = default;     // must
    public U8String? name = null;

    public Buffer()
    {
    }
}

internal struct BufferView
{
    public uint buffer = default;     // must
    public nuint byteOffset = 0;
    public nuint byteLength = default; // must
    public uint? byteStride = null;
    public BufferViewTarget? target = null;
    public U8String? name = null;
    public BufferView()
    {
    }
}

[JsonConverter(typeof(BufferViewTargetConverter))]
internal enum BufferViewTarget
{
    ArrayBuffer,
    ElementArrayBuffer,
}

internal sealed class BufferViewTargetConverter : JsonConverter<BufferViewTarget>
{
    private const int ARRAY_BUFFER = 34962;
    private const int ELEMENT_ARRAY_BUFFER = 34963;

    public override BufferViewTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.Number) { throw new JsonException(); }
        var value = reader.GetInt32();
        return value switch
        {
            ARRAY_BUFFER => BufferViewTarget.ArrayBuffer,
            ELEMENT_ARRAY_BUFFER => BufferViewTarget.ElementArrayBuffer,
            _ => throw new JsonException(),
        };
    }

    public override void Write(Utf8JsonWriter writer, BufferViewTarget value, JsonSerializerOptions options) => throw new NotImplementedException();
}
