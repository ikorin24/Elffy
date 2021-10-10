#nullable enable
using FTV = Elffy.FrameTimingValues;

namespace Elffy
{
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue(nameof(FTV.OutOfFrameLoop), FTV.OutOfFrameLoop)]
    [EnumLikeValue(nameof(FTV.FrameInitializing), FTV.FrameInitializing)]
    [EnumLikeValue(nameof(FTV.EarlyUpdate), FTV.EarlyUpdate)]
    [EnumLikeValue(nameof(FTV.Update), FTV.Update)]
    [EnumLikeValue(nameof(FTV.LateUpdate), FTV.LateUpdate)]
    [EnumLikeValue(nameof(FTV.BeforeRendering), FTV.BeforeRendering)]
    [EnumLikeValue(nameof(FTV.Rendering), FTV.Rendering)]
    [EnumLikeValue(nameof(FTV.AfterRendering), FTV.AfterRendering)]
    [EnumLikeValue(nameof(FTV.FrameFinalizing), FTV.FrameFinalizing)]
    public partial struct CurrentFrameTiming
    {
    }
}
