#nullable enable
using System;
using OpenToolkit.Windowing.GraphicsLibraryFramework;

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
