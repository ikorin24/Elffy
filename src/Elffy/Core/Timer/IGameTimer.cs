#nullable enable
using System;

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
