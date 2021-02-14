#nullable enable
using System.Drawing;
using System.Drawing.Imaging;

namespace Elffy.Imaging
{
    public static class BitmapHelper
    {
        public static BitmapPixels GetPixels(this Bitmap bitmap, ImageLockMode lockMode, PixelFormat format)
        {
            return new BitmapPixels(bitmap, lockMode, format);
        }
    }
}
