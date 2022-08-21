#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf;

internal struct Image
{
    public U8String? uri = null;
    public ImageMimeType? mimeType = null;
    public int? bufferView = null;
    public U8String? name = null;
    public Image()
    {
    }
}

[JsonConverter(typeof(ImageMimeTypeConverter))]
internal enum ImageMimeType
{
    ImageJpeg,
    ImagePng,
}

internal sealed class ImageMimeTypeConverter : JsonConverter<ImageMimeType>
{
    private static ReadOnlySpan<byte> image_jpeg => new byte[10] { (byte)'i', (byte)'m', (byte)'a', (byte)'g', (byte)'e', (byte)'/', (byte)'j', (byte)'p', (byte)'e', (byte)'g' };
    private static ReadOnlySpan<byte> image_png => new byte[9] { (byte)'i', (byte)'m', (byte)'a', (byte)'g', (byte)'e', (byte)'/', (byte)'p', (byte)'n', (byte)'g' };

    public override ImageMimeType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.String) { throw new JsonException(); }
        var value = reader.ValueSpan;
        if(value.SequenceEqual(image_jpeg)) { return ImageMimeType.ImageJpeg; }
        else if(value.SequenceEqual(image_png)) { return ImageMimeType.ImagePng; }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, ImageMimeType value, JsonSerializerOptions options) => throw new NotSupportedException();
}
