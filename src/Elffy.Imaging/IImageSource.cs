#nullable enable
using System;

namespace Elffy.Imaging
{
    public interface IImageSource : IDisposable
    {
        int Width { get; }
        int Height { get; }
        unsafe ColorByte* Pixels { get; }
        short Token { get; }
        Span<ColorByte> GetPixels();
    }

    public static class ImageSourceExtensions
    {
        public unsafe static ImageRef AsImageRef(this IImageSource source)
        {
            return new ImageRef(source.Pixels, source.Width, source.Height);
        }
    }
}
