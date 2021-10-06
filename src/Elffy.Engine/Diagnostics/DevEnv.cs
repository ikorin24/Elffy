#nullable enable
using System.Diagnostics;
using System.Threading;

namespace Elffy.Diagnostics
{
    public static class DevEnv
    {
        private const string NewLine = "\n";

        private static int _isEnabled;
        public static bool IsEnabled => _isEnabled == 1;

        public static void Run()
        {
            if(Interlocked.CompareExchange(ref _isEnabled, 1, 0) == 1) {
                return;
            }

            // Enable diagnostics
            GCTracker.Init();
        }

        public static void Stop()
        {
            if(Interlocked.CompareExchange(ref _isEnabled, 0, 1) == 0) {
                return;
            }

            // Disable diagnostics
            GCTracker.End();
        }

        public static void WriteLine(object? value)
        {
            if(IsEnabled == false) { return; }
            var msg = value?.ToString() + NewLine;
            WritePrivate(msg);
        }

        public static void WriteLine(string? value)
        {
            if(IsEnabled == false) { return; }
            var msg = value + NewLine;
            WritePrivate(msg);
        }

        internal static void ForceWriteLine(string? value, string? category = null)
        {
            WritePrivate(value + NewLine);
        }

        private static void WritePrivate(string message)
        {
            Debugger.Log(0, null, message);
        }
    }
}
