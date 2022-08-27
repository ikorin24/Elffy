#nullable enable
using System;

namespace Elffy.Serialization.Gltf.Parsing;

internal struct Skin
{
    public uint? inverseBindMatrices = null;
    public uint? skeleton = null;
    public uint[] joints = Array.Empty<uint>();  // must
    public U8String? name = null;

    public Skin()
    {
    }
}
