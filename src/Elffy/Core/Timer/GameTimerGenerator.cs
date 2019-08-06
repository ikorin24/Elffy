using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
