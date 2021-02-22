#nullable enable
using System;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.OpenGL
{
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
        public static ClearBufferMask Compat(this ClearMask source) => (ClearBufferMask)source;

        public static BufferTarget Compat(this BufferPackTarget source) => (BufferTarget)source;

        public static BufferUsageHint Compat(this BufferUsage source) => (BufferUsageHint)source;
    }
}
