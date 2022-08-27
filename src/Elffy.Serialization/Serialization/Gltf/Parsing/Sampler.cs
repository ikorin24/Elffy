#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf.Parsing;

internal struct Sampler
{
    public SamplerMagFilter? magFilter = null;
    public SamplerMinFilter? minFilter = null;
    public SamplerWrap wrapS = SamplerWrap.Repeat;
    public SamplerWrap wrapT = SamplerWrap.Repeat;
    public U8String? name = null;

    public Sampler()
    {
    }
}

[JsonConverter(typeof(SamplerMagFilterConverter))]
internal enum SamplerMagFilter
{
    Nearest,
    Linear,
}

internal sealed class SamplerMagFilterConverter : JsonConverter<SamplerMagFilter>
{
    private const int NEAREST = 9728;
    private const int LINEAR = 9729;

    public override SamplerMagFilter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.Number) { throw new JsonException(); }
        var value = reader.GetInt32();
        return value switch
        {
            NEAREST => SamplerMagFilter.Nearest,
            LINEAR => SamplerMagFilter.Linear,
            _ => throw new JsonException(),
        };
    }

    public override void Write(Utf8JsonWriter writer, SamplerMagFilter value, JsonSerializerOptions options) => throw new NotImplementedException();
}

[JsonConverter(typeof(SamplerMinFilterConverter))]
internal enum SamplerMinFilter
{
    Nearest,
    Linear,
    NearestMipmapNearest,
    LinearMipmapNearest,
    NearestMipmapLinear,
    LinearMipmapLinear,
}

internal sealed class SamplerMinFilterConverter : JsonConverter<SamplerMinFilter>
{
    private const int NEAREST = 9728;
    private const int LINEAR = 9729;
    private const int NEAREST_MIPMAP_NEAREST = 9984;
    private const int LINEAR_MIPMAP_NEAREST = 9985;
    private const int NEAREST_MIPMAP_LINEAR = 9986;
    private const int LINEAR_MIPMAP_LINEAR = 9987;

    public override SamplerMinFilter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.Number) { throw new JsonException(); }
        var value = reader.GetInt32();
        return value switch
        {
            NEAREST => SamplerMinFilter.Nearest,
            LINEAR => SamplerMinFilter.Linear,
            NEAREST_MIPMAP_NEAREST => SamplerMinFilter.NearestMipmapNearest,
            LINEAR_MIPMAP_NEAREST => SamplerMinFilter.LinearMipmapNearest,
            NEAREST_MIPMAP_LINEAR => SamplerMinFilter.NearestMipmapLinear,
            LINEAR_MIPMAP_LINEAR => SamplerMinFilter.LinearMipmapLinear,
            _ => throw new JsonException(),
        };
    }

    public override void Write(Utf8JsonWriter writer, SamplerMinFilter value, JsonSerializerOptions options) => throw new NotImplementedException();
}

[JsonConverter(typeof(SamplerWrapConverter))]
internal enum SamplerWrap
{
    ClampToEdge,
    MirroredRepeat,
    Repeat,
}

internal sealed class SamplerWrapConverter : JsonConverter<SamplerWrap>
{
    private const int CLAMP_TO_EDGE = 33071;
    private const int MIRRORED_REPEAT = 33648;
    private const int REPEAT = 10497;

    public override SamplerWrap Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.Number) { throw new JsonException(); }
        var value = reader.GetInt32();
        return value switch
        {
            CLAMP_TO_EDGE => SamplerWrap.ClampToEdge,
            MIRRORED_REPEAT => SamplerWrap.MirroredRepeat,
            REPEAT => SamplerWrap.Repeat,
            _ => throw new JsonException(),
        };
    }

    public override void Write(Utf8JsonWriter writer, SamplerWrap value, JsonSerializerOptions options) => throw new NotImplementedException();
}
