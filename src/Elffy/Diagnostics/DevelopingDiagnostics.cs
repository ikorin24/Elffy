#nullable enable

using Elffy.AssemblyServices;
using System.Diagnostics;

namespace Elffy.Diagnostics
{
    public static class DevelopingDiagnostics
    {
        public static bool IsEnabled { get; private set; }

        [Conditional(AssemblyState.Symbol_Develop)]
        public static void Run()
        {
            if(IsEnabled) { return; }
            IsEnabled = true;

            // Enable diagnostics
            GCTracker.Init();
        }

        [Conditional(AssemblyState.Symbol_Develop)]
        public static void Stop()
        {
            IsEnabled = false;

            // Disable diagnostics
            GCTracker.End();
        }
    }
}
