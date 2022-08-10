#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    public static class HostScreenExtensions
    {
        public static void ThrowIfCurrentTimingOufOfFrame(this IHostScreen screen)
        {
            if(screen.CurrentTiming == CurrentFrameTiming.OutOfFrameLoop) {
                Throw();

                [DoesNotReturn] static void Throw() => throw new InvalidOperationException("Current timing is out of frame loop or invalid thread.");
            }
        }
    }
}
