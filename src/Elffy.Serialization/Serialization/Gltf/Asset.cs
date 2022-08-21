#nullable enable

namespace Elffy.Serialization.Gltf;

internal struct Asset
{
    public U8String? copyright = null;
    public U8String? generator = null;
    public U8String version = default;    // must
    public U8String? minVersion = null;
    public Asset()
    {
    }
}
