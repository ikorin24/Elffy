#nullable enable
using System;
using OpenTK.Graphics.OpenGL4;
using Elffy;

namespace Elffy.Graphics.OpenGL
{
    [GenerateEnumLikeStruct(typeof(int))]
    [EnumLikeValue(nameof(PixelInternalFormat.Rgba8), (int)PixelInternalFormat.Rgba8, "public", "(R8 G8 B8 A8), each channel is unsigned byte (0 ~ 255)")]
    [EnumLikeValue(nameof(PixelInternalFormat.Rgba16f), (int)PixelInternalFormat.Rgba16f, "public", "(R16 G16 B16 A16), each channel is 16bit floating point value")]
    [EnumLikeValue(nameof(PixelInternalFormat.Rgba32f), (int)PixelInternalFormat.Rgba32f, "public", "(R32 G32 B32 A32), each channel is 32bit floating point value")]
    [EnumLikeValue(nameof(PixelInternalFormat.DepthComponent), (int)PixelInternalFormat.DepthComponent, "public", "GL_DEPTH_COMPONENT. Use texture as a depth buffer. The driver chooses its precision.")]
    [EnumLikeValue(nameof(PixelInternalFormat.DepthComponent16), (int)PixelInternalFormat.DepthComponent16, "public", "GL_DEPTH_COMPONENT16. Use texture as a depth buffer. 16 bits precision.")]
    [EnumLikeValue(nameof(PixelInternalFormat.DepthComponent24), (int)PixelInternalFormat.DepthComponent24, "public", "GL_DEPTH_COMPONENT24. Use texture as a depth buffer. 24 bits precision.")]
    public partial struct TextureInternalFormat
    {
        internal PixelInternalFormat ToOriginalValue() => (PixelInternalFormat)_value;
    }

    [Flags]
    public enum ClearMask
    {
        /// <summary>GL_NONE</summary>
        None = ClearBufferMask.None,
        /// <summary>GL_DEPTH_BUFFER_BIT</summary>
        DepthBufferBit = ClearBufferMask.DepthBufferBit,
        /// <summary>GL_ACCUM_BUFFER_BIT</summary>
        AccumBufferBit = ClearBufferMask.AccumBufferBit,
        /// <summary>GL_STENCIL_BUFFER_BIT</summary>
        StencilBufferBit = ClearBufferMask.StencilBufferBit,
        /// <summary>GL_COLOR_BUFFER_BIT</summary>
        ColorBufferBit = ClearBufferMask.ColorBufferBit,
    }

    [GenerateEnumLikeStruct(typeof(int))]
    [EnumLikeValue(nameof(BufferTarget.PixelPackBuffer), (int)BufferTarget.PixelPackBuffer, "public", "GL_PIXEL_PACK_BUFFER = 0x88EB")]
    [EnumLikeValue(nameof(BufferTarget.PixelUnpackBuffer), (int)BufferTarget.PixelUnpackBuffer, "public", "GL_PIXEL_UNPACK_BUFFER = 0x88EC")]
    public partial struct BufferPackTarget
    {
        internal BufferTarget ToOriginalValue() => (BufferTarget)_value;
    }

    [GenerateEnumLikeStruct(typeof(int))]
    [EnumLikeValue(nameof(BufferAccess.ReadOnly), (int)BufferAccess.ReadOnly, "public", "GL_READ_ONLY = 0x88B8")]
    [EnumLikeValue(nameof(BufferAccess.WriteOnly), (int)BufferAccess.WriteOnly, "public", "GL_WRITE_ONLY = 0x88B9")]
    [EnumLikeValue(nameof(BufferAccess.ReadWrite), (int)BufferAccess.ReadWrite, "public", "GL_READ_WRITE = 0x88BA")]
    public partial struct BufferAccessMode
    {
        internal BufferAccess ToOriginalValue() => (BufferAccess)_value;
    }

    internal static class EnumCompatibleCastExtension
    {
        //public static PixelInternalFormat Compat(this TextureInternalFormat source) => (PixelInternalFormat)source;

        public static ClearBufferMask Compat(this ClearMask source) => (ClearBufferMask)source;

        //public static BufferTarget Compat(this BufferPackTarget source) => (BufferTarget)source;
    }
}
