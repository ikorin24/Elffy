#nullable enable
using OpenTK.Graphics.OpenGL4;

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

    [GenerateEnumLikeStruct(typeof(int))]
    [EnumLikeValue(nameof(ClearBufferMask.None), (int)ClearBufferMask.None, "public", "GL_NONE = 0")]
    [EnumLikeValue(nameof(ClearBufferMask.DepthBufferBit), (int)ClearBufferMask.DepthBufferBit, "public", "GL_DEPTH_BUFFER_BIT = 0x00000100")]
    [EnumLikeValue(nameof(ClearBufferMask.AccumBufferBit), (int)ClearBufferMask.AccumBufferBit, "public", "GL_ACCUM_BUFFER_BIT = 0x00000200")]
    [EnumLikeValue(nameof(ClearBufferMask.StencilBufferBit), (int)ClearBufferMask.StencilBufferBit, "public", "GL_STENCIL_BUFFER_BIT = 0x00000400")]
    [EnumLikeValue(nameof(ClearBufferMask.ColorBufferBit), (int)ClearBufferMask.ColorBufferBit, "public", "GL_COLOR_BUFFER_BIT = 0x00004000")]
    public partial struct ClearMask
    {
        internal ClearBufferMask ToOriginalValue() => (ClearBufferMask)_value;

        // TODO: Auto generate implementation for flags

        public bool HasFlag(ClearMask value) => (_value | value._value) == value._value;

        public static ClearMask operator |(ClearMask left, ClearMask right) => new ClearMask(left._value | right._value);
        public static ClearMask operator &(ClearMask left, ClearMask right) => new ClearMask(left._value & right._value);
        public static ClearMask operator ~(ClearMask value) => new ClearMask(~value._value);
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
}
