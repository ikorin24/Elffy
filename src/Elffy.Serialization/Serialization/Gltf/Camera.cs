#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf;

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
    private static ReadOnlySpan<byte> perspective => new byte[11] { (byte)'p', (byte)'e', (byte)'r', (byte)'s', (byte)'p', (byte)'e', (byte)'c', (byte)'t', (byte)'i', (byte)'v', (byte)'e' };
    private static ReadOnlySpan<byte> orthographic => new byte[12] { (byte)'o', (byte)'r', (byte)'t', (byte)'h', (byte)'o', (byte)'g', (byte)'r', (byte)'a', (byte)'p', (byte)'h', (byte)'i', (byte)'c' };

    public override CameraType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.String) { throw new JsonException(); }
        var value = reader.ValueSpan;
        if(value.SequenceEqual(perspective)) { return CameraType.Perspective; }
        else if(value.SequenceEqual(orthographic)) { return CameraType.Orthographic; }
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
