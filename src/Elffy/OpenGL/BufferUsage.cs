#nullable enable
using OpenTK.Graphics.OpenGL4;

namespace Elffy.OpenGL
{
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

    public static class BufferUsageExtension
    {
        public static BufferUsageHint ToBufferUsageHint(this BufferUsage source) => (BufferUsageHint)source;
    }
}
