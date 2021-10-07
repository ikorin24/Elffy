#nullable enable

namespace Elffy
{
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue("OutOfFrameLoop", 0)]
    [EnumLikeValue("EarlyUpdate", 1)]
    [EnumLikeValue("Update", 2)]
    [EnumLikeValue("LateUpdate", 3)]
    [EnumLikeValue("BeforeRendering", 4)]
    [EnumLikeValue("Rendering", 100)]
    [EnumLikeValue("AfterRendering", 5)]
    [EnumLikeValue("FrameInitializing", 101)]
    [EnumLikeValue("FrameFinalizing", 102)]
    public partial struct CurrentFrameTiming
    {
    }
}
