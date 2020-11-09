#nullable enable
using Elffy.Imaging;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnitTest
{
    public class RawImageTest
    {
        [Fact]
        public unsafe void RawImageLayout()
        {
            // Layout of Elffy.Imaging.RawImage must be same as OpenTK.Windowing.GraphicsLibraryFramework.Image

            Assert.Equal(sizeof(Image), sizeof(RawImage));
            var rawImage = new RawImage();
            var image = new Image();
            Assert.Equal((byte*)&image.Width - (byte*)&image, (byte*)&rawImage.Width - (byte*)&rawImage);
            Assert.Equal((byte*)&image.Height - (byte*)&image, (byte*)&rawImage.Height - (byte*)&rawImage);
            Assert.Equal((byte*)&image.Pixels - (byte*)&image, (byte*)&rawImage.Pixels - (byte*)&rawImage);
        }
    }
}
