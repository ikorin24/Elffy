#nullable enable
using System;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Elffy.OpenGL.Windowing
{
    internal readonly ref struct WindowIconRaw
    {
        public readonly ReadOnlySpan<Image> Images;

        public static WindowIconRaw Empty => default;

        public WindowIconRaw(Span<Image> images)
        {
            Images = images;
        }
    }
}
