#nullable enable
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Graphics.OpenGL
{
    /// <summary>A hint of buffer usage. However, most OpenGL drivers nowadays either don't use it or ignore it, so you don't need to worry about it.</summary>
    /// <remarks>
    /// You don't need to worry about it because most OpenGL drivers nowadays either don't use it or ignore it.
    /// </remarks>
    [GenerateEnumLikeStruct(typeof(int))]
    [EnumLikeValue(nameof(BufferUsageHint.StreamDraw), (int)BufferUsageHint.StreamDraw, "public", "GL_STREAM_DRAW = 0x88E0 (Every frame, application -> GL)")]
    [EnumLikeValue(nameof(BufferUsageHint.StreamRead), (int)BufferUsageHint.StreamRead, "public", "GL_STREAM_READ = 0x88E1 (Every frame, GL -> application)")]
    [EnumLikeValue(nameof(BufferUsageHint.StreamCopy), (int)BufferUsageHint.StreamCopy, "public", "GL_STREAM_COPY = 0x88E2 (Every frame, GL -> GL)")]
    [EnumLikeValue(nameof(BufferUsageHint.StaticDraw), (int)BufferUsageHint.StaticDraw, "public", "GL_STATIC_DRAW = 0x88E4 (Modified once on initializing, application -> GL)")]
    [EnumLikeValue(nameof(BufferUsageHint.StaticRead), (int)BufferUsageHint.StaticRead, "public", "GL_STATIC_READ = 0x88E5 (Modified once on initializing, GL -> application)")]
    [EnumLikeValue(nameof(BufferUsageHint.StaticCopy), (int)BufferUsageHint.StaticCopy, "public", "GL_STATIC_COPY = 0x88E6 (Modified once on initializing, GL -> GL)")]
    [EnumLikeValue(nameof(BufferUsageHint.DynamicDraw), (int)BufferUsageHint.DynamicDraw, "public", "GL_DYNAMIC_DRAW = 0x88E8 (Frequently, application -> GL)")]
    [EnumLikeValue(nameof(BufferUsageHint.DynamicRead), (int)BufferUsageHint.DynamicRead, "public", "GL_DYNAMIC_READ = 0x88E9 (Frequently, GL -> application)")]
    [EnumLikeValue(nameof(BufferUsageHint.DynamicCopy), (int)BufferUsageHint.DynamicCopy, "public", "GL_DYNAMIC_COPY = 0x88EA (Frequently, GL -> GL)")]
    public readonly partial struct BufferHint
    {
        internal BufferUsageHint ToOriginalValue() => (BufferUsageHint)_value;
    }
}
