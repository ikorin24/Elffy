using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core.Timer
{
    internal interface IGameTimer
    {
        void Start();
        void Stop();
        void Reset();
        TimeSpan Elapsed { get; }
        long ElapsedMilliseconds { get; }
        bool IsRunning { get; }
    }
}
