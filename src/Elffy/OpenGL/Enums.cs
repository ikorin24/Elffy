#nullable enable
using System;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.OpenGL
{
    public enum TextureInternalFormat
    {
        /// <summary>(R8 G8 B8 A8), each channel is unsigned byte (0 ~ 255)</summary>
        Rgba8 = PixelInternalFormat.Rgba8,
        /// <summary>(R16 G16 B16 A16), each channel is 16bit floating point value</summary>
        Rgba16f = PixelInternalFormat.Rgba16f,
        /// <summary>(R32 G32 B32 A32), each channel is 32bit floating point value</summary>
        Rgba32f = PixelInternalFormat.Rgba32f,

        /// <summary>GL_DEPTH_COMPONENT. Use texture as a depth buffer. The driver chooses its precision.</summary>
        DepthComponent = PixelInternalFormat.DepthComponent,
        /// <summary>GL_DEPTH_COMPONENT16. Use texture as a depth buffer. 16 bits precision.</summary>
        DepthComponent16 = PixelInternalFormat.DepthComponent16,
        /// <summary>GL_DEPTH_COMPONENT24. Use texture as a depth buffer. 24 bits precision.</summary>
        DepthComponent24 = PixelInternalFormat.DepthComponent24,
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

    public enum BufferPackTarget
    {
        PixelPackBuffer = BufferTarget.PixelPackBuffer,
        PixelUnpackBuffer = BufferTarget.PixelUnpackBuffer,
    }

    public enum BufferUsage
    {
        /// <summary>Every frame, application -> GL</summary>
        StreamDraw = BufferUsageHint.StreamDraw,
        /// <summary>One time on initializing, application -> GL</summary>
        StaticDraw = BufferUsageHint.StaticDraw,
        /// <summary>Frequently, application -> GL</summary>
        DynamicDraw = BufferUsageHint.DynamicDraw,

        //StreamRead = BufferUsageHint.StreamRead,
        //StreamCopy = BufferUsageHint.StreamCopy,
        //StaticRead = BufferUsageHint.StaticRead,
        //StaticCopy = BufferUsageHint.StaticCopy,
        //DynamicRead = BufferUsageHint.DynamicRead,
        //DynamicCopy = BufferUsageHint.DynamicCopy,
    }

    internal static class EnumCompatibleCastExtension
    {
        public static PixelInternalFormat Compat(this TextureInternalFormat source) => (PixelInternalFormat)source;

        public static ClearBufferMask Compat(this ClearMask source) => (ClearBufferMask)source;

        public static BufferTarget Compat(this BufferPackTarget source) => (BufferTarget)source;

        public static BufferUsageHint Compat(this BufferUsage source) => (BufferUsageHint)source;
    }
}
