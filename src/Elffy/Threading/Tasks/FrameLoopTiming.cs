#nullable enable

namespace Elffy.Threading.Tasks
{
    public enum FrameLoopTiming
    {
        EarlyUpdate,
        Update,
        LateUpdate,
        BeforeRendering,
        AfterRendering,
    }
}
