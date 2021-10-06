#nullable enable

namespace Elffy
{
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue("OutOfFrameLoop", 0)]
    [EnumLikeValue("EarlyUpdate", 1)]
    [EnumLikeValue("Update", 2)]
    [EnumLikeValue("LateUpdate", 3)]
    [EnumLikeValue("BeforeRendering", 4)]
    [EnumLikeValue("AfterRendering", 5)]
    [EnumLikeValue("FrameInitializing", 100)]
    [EnumLikeValue("FrameFinalizing", 101)]
    public partial struct CurrentFrameTiming
    {
    }
}
