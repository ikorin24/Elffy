#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf.Parsing;

internal struct Image
{
    public U8String? uri = null;
    public ImageMimeType? mimeType = null;
    public uint? bufferView = null;
    public U8String? name = null;
    public Image()
    {
    }
}

[JsonConverter(typeof(ImageMimeTypeConverter))]
internal enum ImageMimeType
{
    /// <summary>image/jpeg</summary>
    ImageJpeg,
    /// <summary>image/png</summary>
    ImagePng,
}

internal sealed class ImageMimeTypeConverter : JsonConverter<ImageMimeType>
{
    public override ImageMimeType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.String) { throw new JsonException(); }
        var value = reader.ValueSpan;
        if(value.SequenceEqual("image/jpeg"u8)) { return ImageMimeType.ImageJpeg; }
        else if(value.SequenceEqual("image/png"u8)) { return ImageMimeType.ImagePng; }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, ImageMimeType value, JsonSerializerOptions options) => throw new NotSupportedException();
}
