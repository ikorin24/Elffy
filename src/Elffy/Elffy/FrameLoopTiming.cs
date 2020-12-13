#nullable enable

namespace Elffy
{
    public enum FrameLoopTiming : byte
    {
        EarlyUpdate = 1,
        Update = 2,
        LateUpdate = 3,
        BeforeRendering = 4,
        AfterRendering = 5,
    }

    public enum ScreenCurrentTiming : byte
    {
        OutOfFrameLoop = 0, // default value must be 'OutOfFrameLoop'

        EarlyUpdate = 1,
        Update = 2,
        LateUpdate = 3,
        BeforeRendering = 4,
        AfterRendering = 5,

        FrameInitializing = byte.MaxValue,
        FrameFinalizing = byte.MinValue,
    }
}
