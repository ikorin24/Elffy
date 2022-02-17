#nullable enable
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Graphics.OpenGL
{
    [GenerateEnumLikeStruct(typeof(int))]
    [EnumLikeValue(nameof(StreamDraw), (int)BufferUsageHint.StreamDraw, "public", "GL_STREAM_DRAW = 0x88E0")]       // Every frame, application -> GL
    [EnumLikeValue(nameof(StreamRead), (int)BufferUsageHint.StreamRead, "public", "GL_STREAM_READ = 0x88E1")]
    [EnumLikeValue(nameof(StreamCopy), (int)BufferUsageHint.StreamCopy, "public", "GL_STREAM_COPY = 0x88E2")]
    [EnumLikeValue(nameof(StaticDraw), (int)BufferUsageHint.StaticDraw, "public", "GL_STATIC_DRAW = 0x88E4")]       // One time on initializing, application -> GL
    [EnumLikeValue(nameof(StaticRead), (int)BufferUsageHint.StaticRead, "public", "GL_STATIC_READ = 0x88E5")]
    [EnumLikeValue(nameof(StaticCopy), (int)BufferUsageHint.StaticCopy, "public", "GL_STATIC_COPY = 0x88E6")]
    [EnumLikeValue(nameof(DynamicDraw), (int)BufferUsageHint.DynamicDraw, "public", "GL_DYNAMIC_DRAW = 0x88E8")]    // Frequently, application -> GL
    [EnumLikeValue(nameof(DynamicRead), (int)BufferUsageHint.DynamicRead, "public", "GL_DYNAMIC_READ = 0x88E9")]
    [EnumLikeValue(nameof(DynamicCopy), (int)BufferUsageHint.DynamicCopy, "public", "GL_DYNAMIC_COPY = 0x88EA")]
    public readonly partial struct BufferHint
    {
        internal BufferUsageHint ToOriginalValue() => (BufferUsageHint)_value;
    }
}
