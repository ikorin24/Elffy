﻿#nullable enable

namespace Elffy.Serialization.Gltf.Parsing;

internal struct Texture
{
    public uint? sampler = null;
    public uint? source = null;
    public U8String? name = null;
    public Texture()
    {
    }
}
