#nullable enable

namespace Elffy.Imaging
{
    internal enum TgaDataFormat : byte
    {
        NoImage = 0,
        IndexedColor = 1,
        FullColor = 2,
        Gray = 3,
        IndexedColorRLE = 9,
        FullColorRLE = 10,
        GrayRLE = 11,
    }
}
