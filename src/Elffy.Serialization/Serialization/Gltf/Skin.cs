#nullable enable
using System;

namespace Elffy.Serialization.Gltf;

internal struct Skin
{
    public int? inverseBindMatrices = null;
    public int? skeleton = null;
    public int[] joints = Array.Empty<int>();  // must
    public U8String? name = null;

    public Skin()
    {
    }
}
