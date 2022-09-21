#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf.Parsing;

internal struct Camera
{
    public CameraOrthographic? orthographic = null;
    public CameraPerspective? perspective = null;
    public CameraType type = default;   // must
    public U8String? name = null;
    public Camera()
    {
    }
}

[JsonConverter(typeof(CameraTypeConverter))]
internal enum CameraType
{
    Perspective,
    Orthographic,
}

internal sealed class CameraTypeConverter : JsonConverter<CameraType>
{
    public override CameraType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.String) { throw new JsonException(); }
        var value = reader.ValueSpan;
        if(value.SequenceEqual("perspective"u8)) { return CameraType.Perspective; }
        else if(value.SequenceEqual("orthographic"u8)) { return CameraType.Orthographic; }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, CameraType value, JsonSerializerOptions options) => throw new NotSupportedException();
}

internal struct CameraOrthographic
{
    public float xmag = default;    // must
    public float ymag = default;    // must
    public float zfar = default;    // must
    public float znear = default;   // must
    public CameraOrthographic()
    {
    }
}

internal struct CameraPerspective
{
    public float? aspectRatio = null;
    public float yfov = default;    // must
    public float? zfar = null;
    public float znear = default;   // must
    public CameraPerspective()
    {
    }
}
