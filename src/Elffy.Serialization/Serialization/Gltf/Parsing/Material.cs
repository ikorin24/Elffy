#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elffy.Serialization.Gltf.Parsing;

internal struct Material
{
    public U8String? name = null;
    public MaterialPbrMetallicRoughness? pbrMetallicRoughness = null;
    public MaterialNormalTextureInfo? normalTexture = null;
    public MaterialOcclusionTextureInfo? occlusionTexture = null;
    public TextureInfo? emissiveTexture = null;
    public Vector3 emissiveFactor = new Vector3();
    public MaterialAlphaMode alphaMode = MaterialAlphaMode.Opaque;
    public float alphaCutoff = 0.5f;
    public bool doubleSided = false;

    public Material()
    {
    }
}

[JsonConverter(typeof(MaterialAlphaModeConverter))]
internal enum MaterialAlphaMode
{
    Opaque,
    Mask,
    Blend,
}

internal sealed class MaterialAlphaModeConverter : JsonConverter<MaterialAlphaMode>
{
    private static ReadOnlySpan<byte> OPAQUE => new byte[6] { (byte)'O', (byte)'P', (byte)'A', (byte)'Q', (byte)'U', (byte)'E' };
    private static ReadOnlySpan<byte> MASK => new byte[4] { (byte)'M', (byte)'A', (byte)'S', (byte)'K' };
    private static ReadOnlySpan<byte> BLEND => new byte[5] { (byte)'B', (byte)'L', (byte)'E', (byte)'N', (byte)'D' };

    public override MaterialAlphaMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if(reader.TokenType != JsonTokenType.String) { throw new JsonException(); }
        var value = reader.ValueSpan;
        if(value.SequenceEqual(OPAQUE)) { return MaterialAlphaMode.Opaque; }
        else if(value.SequenceEqual(MASK)) { return MaterialAlphaMode.Mask; }
        else if(value.SequenceEqual(BLEND)) { return MaterialAlphaMode.Blend; }
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, MaterialAlphaMode value, JsonSerializerOptions options) => throw new NotSupportedException();
}

internal struct MaterialPbrMetallicRoughness
{
    public Vector4 baseColorFactor = new Vector4(1, 1, 1, 1);
    public TextureInfo? baseColorTexture = null;
    public float metallicFactor = 1;
    public float roughnessFactor = 1;
    public TextureInfo? metallicRoughnessTexture = null;

    public MaterialPbrMetallicRoughness()
    {
    }
}

internal struct TextureInfo
{
    public uint index = default;  // must
    public uint texCoord = 0;

    public TextureInfo()
    {
    }
}

internal struct MaterialNormalTextureInfo
{
    public uint index = default;  // must
    public uint texCoord = 0;
    public float scale = 1;

    public MaterialNormalTextureInfo()
    {
    }
}

internal struct MaterialOcclusionTextureInfo
{
    public uint index = default;  // must
    public uint texCoord = 0;
    public float strength = 1;

    public MaterialOcclusionTextureInfo()
    {
    }
}
