#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf.Parsing;

[JsonConverter(typeof(QuaternionConverter))]
internal struct Quaternion
{
    public float X;
    public float Y;
    public float Z;
    public float W;

    public static Quaternion Identity => new Quaternion(0, 0, 0, 1);

    public Quaternion(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
}

internal sealed class QuaternionConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // expected [number, number, number, number]

        if(reader.TokenType != JsonTokenType.StartArray) { throw new JsonException(); }
        reader.Read();
        if(reader.TokenType != JsonTokenType.Number) { throw new JsonException(); }
        Quaternion value;
        value.X = reader.GetSingle();
        reader.Read();
        value.Y = reader.GetSingle();
        reader.Read();
        value.Z = reader.GetSingle();
        reader.Read();
        value.W = reader.GetSingle();
        reader.Read();

        if(reader.TokenType != JsonTokenType.EndArray) { throw new JsonException(); }
        return value;
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options) => throw new NotSupportedException();
}
