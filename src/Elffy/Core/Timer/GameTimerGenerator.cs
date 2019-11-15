#nullable enable
using System.Diagnostics;

namespace Elffy.Core.Timer
{
    internal static class GameTimerGenerator
    {
        public static IGameTimer Create()
        {
#if DEBUG
            var timer = Debugger.IsAttached ? new DebugGameTimer() : (IGameTimer)new GameTimer();
#else
            var timer = new GameTimer();
#endif
            return timer;
        }
    }
}
