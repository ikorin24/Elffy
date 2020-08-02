#nullable enable

namespace Elffy.Diagnostics
{
    public static class DiagnosticsSetting
    {
        private static bool _isRunning;

        public static bool IsEnableDiagnostics { get; private set; }

        public static void Run()
        {
            if(_isRunning) { return; }
            IsEnableDiagnostics = true;

            // Enable diagnostics
            GCTracker.Init();
        }

        public static void Stop()
        {
            _isRunning = false;
            IsEnableDiagnostics = false;

            // Disable diagnostics
            GCTracker.End();
        }
    }
}
