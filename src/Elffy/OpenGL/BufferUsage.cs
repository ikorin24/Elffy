#nullable enable
using OpenTK.Graphics.OpenGL4;

namespace Elffy.OpenGL
{
    public enum BufferUsage
    {
        StreamDraw = BufferUsageHint.StreamDraw,
        StaticDraw = BufferUsageHint.StaticDraw,
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
