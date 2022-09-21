#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf.Parsing;

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
    public uint sampler = default;    // must
    public AnimationChannelTarget target = new();  // must

    public AnimationChannel()
    {
    }
}

internal struct AnimationChannelTarget
{
    public uint? node = null;
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
    public override AnimationChannelTargetPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.String) {
            throw new JsonException();
        }
        var value = reader.ValueSpan;
        if(value.SequenceEqual("translation"u8)) { return AnimationChannelTargetPath.Translation; }
        else if(value.SequenceEqual("rotation"u8)) { return AnimationChannelTargetPath.Rotation; }
        else if(value.SequenceEqual("scale"u8)) { return AnimationChannelTargetPath.Scale; }
        else if(value.SequenceEqual("weights"u8)) { return AnimationChannelTargetPath.Weights; }
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
    public override AnimationSamplerInterpolation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.String) {
            throw new JsonException();
        }
        var value = reader.ValueSpan;
        if(value.SequenceEqual("LINEAR"u8)) { return AnimationSamplerInterpolation.Linear; }
        else if(value.SequenceEqual("STEP"u8)) { return AnimationSamplerInterpolation.Step; }
        else if(value.SequenceEqual("CUBICSPLINE"u8)) { return AnimationSamplerInterpolation.CubicSpline; }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, AnimationSamplerInterpolation value, JsonSerializerOptions options) => throw new NotSupportedException();
}
