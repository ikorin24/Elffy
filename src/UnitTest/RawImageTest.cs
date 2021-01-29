#nullable enable
using Elffy;
using Elffy.Imaging;
using Elffy.Effective.Unsafes;
using Xunit;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.CompilerServices;

namespace UnitTest
{
    public class RawImageTest
    {
        [Fact]
        public unsafe void RawImageLayout()
        {
            // Layout of Elffy.Imaging.RawImage must be same as OpenTK.Windowing.GraphicsLibraryFramework.Image
            {
                Assert.Equal(sizeof(Image), sizeof(RawImage));
                var rawImage = new RawImage();
                var image = new Image();
                Assert.Equal((byte*)&image.Width - (byte*)&image, (byte*)&rawImage.Width - (byte*)&rawImage);
                Assert.Equal((byte*)&image.Height - (byte*)&image, (byte*)&rawImage.Height - (byte*)&rawImage);
                Assert.Equal((byte*)&image.Pixels - (byte*)&image, (byte*)&rawImage.Pixels - (byte*)&rawImage);
            }


            // Layout of RawImage must be same as ReadOnlyRawImage.
            {
                Assert.Equal(sizeof(RawImage), sizeof(ReadOnlyRawImage));
                var pixels = stackalloc ColorByte[60];
                var rawImage = new RawImage(10, 6, pixels);
                ref var readOnlyRawImage = ref Unsafe.As<RawImage, ReadOnlyRawImage>(ref rawImage);
                Assert.True(UnsafeEx.AreSame(in rawImage.Width, in readOnlyRawImage.Width));
                Assert.True(UnsafeEx.AreSame(in rawImage.Height, in readOnlyRawImage.Height));
                Assert.True(rawImage.Pixels == UnsafeEx.AsPointer(in readOnlyRawImage.Pixels));
            }
        }
    }
}
