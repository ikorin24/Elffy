#nullable enable

namespace Elffy.Serialization.Gltf;

internal struct Texture
{
    public int? sampler = null;
    public int? source = null;
    public U8String? name = null;
    public Texture()
    {
    }
}
