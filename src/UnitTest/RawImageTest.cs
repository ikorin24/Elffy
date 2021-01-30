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
            CheckLayout1();
            static void CheckLayout1()
            {
                Assert.Equal(sizeof(Image), sizeof(RawImage));
                var image = new Image();
                ref var rawImage = ref Unsafe.As<Image, RawImage>(ref image);
                Assert.True(UnsafeEx.AreSame(in rawImage.Width, in image.Width));
                Assert.True(UnsafeEx.AreSame(in rawImage.Height, in image.Height));
                Assert.True(Unsafe.AsPointer(ref rawImage.Pixels) == image.Pixels);
            }


            // Layout of RawImage must be same as ReadOnlyRawImage.
            CheckLayout2();
            static void CheckLayout2()
            {
                Assert.Equal(sizeof(RawImage), sizeof(ReadOnlyRawImage));
                var pixels = stackalloc ColorByte[60];
                var rawImage = new RawImage(10, 6, pixels);
                ref var readOnlyRawImage = ref Unsafe.As<RawImage, ReadOnlyRawImage>(ref rawImage);
                Assert.True(UnsafeEx.AreSame(in rawImage.Width, in readOnlyRawImage.Width));
                Assert.True(UnsafeEx.AreSame(in rawImage.Height, in readOnlyRawImage.Height));
                Assert.True(UnsafeEx.AreSame(in rawImage.Pixels, in readOnlyRawImage.Pixels));
            }

            // Layout of RawImageF must be same as ReadOnlyRawImageF.
            CheckLayout3();
            static void CheckLayout3()
            {
                Assert.Equal(sizeof(RawImageF), sizeof(ReadOnlyRawImageF));
                var pixels = stackalloc Color4[60];
                var rawImageF = new RawImageF(10, 6, pixels);
                ref var readOnlyRawImageF = ref Unsafe.As<RawImageF, ReadOnlyRawImageF>(ref rawImageF);
                Assert.True(UnsafeEx.AreSame(in rawImageF.Width, in readOnlyRawImageF.Width));
                Assert.True(UnsafeEx.AreSame(in rawImageF.Height, in readOnlyRawImageF.Height));
                Assert.True(UnsafeEx.AreSame(in rawImageF.Pixels, in readOnlyRawImageF.Pixels));
            }
        }
    }
}
