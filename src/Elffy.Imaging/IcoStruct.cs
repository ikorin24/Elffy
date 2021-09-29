#nullable enable

namespace Elffy.Imaging
{
#pragma warning disable 0649
    internal struct ICONDIR
    {
        public ushort idReserved;  // Reserved (must be 0)
        public ushort idType;      // Resource Type (1 for icons)
        public ushort idCount;     // How many images?
    }

    internal struct ICONDIRENTRY
    {
        public byte bWidth;          // Width, in pixels, of the image
        public byte bHeight;         // Height, in pixels, of the image
        public byte bColorCount;     // Number of colors in image (0 if >=8bpp)
        public byte bReserved;       // Reserved ( must be 0)
        public ushort wPlanes;         // Color Planes
        public ushort wBitCount;       // Bits per pixel
        public uint dwBytesInRes;    // How many bytes in this resource?
        public uint dwImageOffset;   // Where in the file is this image?
    }

    internal struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    };

    internal struct RGBQUAD
    {
        public byte b;
        public byte g;
        public byte r;
        public byte reserved;
    }
#pragma warning restore 0649
}
