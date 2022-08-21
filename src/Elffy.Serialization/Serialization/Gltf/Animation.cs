#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf;

internal struct Animation
{
    private static readonly AnimationChannel[] _emptyChannels = new AnimationChannel[0];
    private static readonly AnimationSampler[] _emptySamplers = new AnimationSampler[0];

    public AnimationChannel[] channels = _emptyChannels;  // must
    public AnimationSampler[]? samplers = _emptySamplers; // must
    public U8String? name = null;

    public Animation()
    {
    }
}

internal struct AnimationChannel
{
    public int sampler = default;    // must
    public AnimationChannelTarget target = new();  // must

    public AnimationChannel()
    {
    }
}

internal struct AnimationChannelTarget
{
    public int? node = null;
    public AnimationChannelTargetPath path = default;    // must
    public AnimationChannelTarget()
    {
    }
}

[JsonConverter(typeof(AnimationChannelTargetPathConverter))]
internal enum AnimationChannelTargetPath
{
    Translation,
    Rotation,
    Scale,
    Weights,
}

internal sealed class AnimationChannelTargetPathConverter : JsonConverter<AnimationChannelTargetPath>
{
    private static ReadOnlySpan<byte> translation => new byte[11] { (byte)'t', (byte)'r', (byte)'a', (byte)'n', (byte)'s', (byte)'l', (byte)'a', (byte)'t', (byte)'i', (byte)'o', (byte)'n' };
    private static ReadOnlySpan<byte> rotation => new byte[8] { (byte)'r', (byte)'o', (byte)'t', (byte)'a', (byte)'t', (byte)'i', (byte)'o', (byte)'n' };
    private static ReadOnlySpan<byte> scale => new byte[5] { (byte)'s', (byte)'c', (byte)'a', (byte)'l', (byte)'e' };
    private static ReadOnlySpan<byte> weights => new byte[7] { (byte)'w', (byte)'e', (byte)'i', (byte)'g', (byte)'h', (byte)'t', (byte)'s' };

    public override AnimationChannelTargetPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.String) {
            throw new JsonException();
        }
        var value = reader.ValueSpan;
        if(value.SequenceEqual(translation)) { return AnimationChannelTargetPath.Translation; }
        else if(value.SequenceEqual(rotation)) { return AnimationChannelTargetPath.Rotation; }
        else if(value.SequenceEqual(scale)) { return AnimationChannelTargetPath.Scale; }
        else if(value.SequenceEqual(weights)) { return AnimationChannelTargetPath.Weights; }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, AnimationChannelTargetPath value, JsonSerializerOptions options) => throw new NotSupportedException();
}

internal struct AnimationSampler
{
    public int input = default;  // must
    public AnimationSamplerInterpolation interpolation = AnimationSamplerInterpolation.Linear;
    public int output = default; // must
    public AnimationSampler()
    {
    }
}

[JsonConverter(typeof(AnimationSamplerInterpolationConverter))]
internal enum AnimationSamplerInterpolation
{
    Linear,
    Step,
    CubicSpline,
}

internal sealed class AnimationSamplerInterpolationConverter : JsonConverter<AnimationSamplerInterpolation>
{
    private static ReadOnlySpan<byte> LINEAR => new byte[] { (byte)'L', (byte)'I', (byte)'N', (byte)'E', (byte)'A', (byte)'R' };
    private static ReadOnlySpan<byte> STEP => new byte[] { (byte)'S', (byte)'T', (byte)'E', (byte)'P' };
    private static ReadOnlySpan<byte> CUBICSPLINE => new byte[] { (byte)'C', (byte)'U', (byte)'B', (byte)'I', (byte)'C', (byte)'S', (byte)'P', (byte)'L', (byte)'I', (byte)'N', (byte)'E' };

    public override AnimationSamplerInterpolation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.String) {
            throw new JsonException();
        }
        var value = reader.ValueSpan;
        if(value.SequenceEqual(LINEAR)) { return AnimationSamplerInterpolation.Linear; }
        else if(value.SequenceEqual(STEP)) { return AnimationSamplerInterpolation.Step; }
        else if(value.SequenceEqual(CUBICSPLINE)) { return AnimationSamplerInterpolation.CubicSpline; }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, AnimationSamplerInterpolation value, JsonSerializerOptions options) => throw new NotSupportedException();
}
