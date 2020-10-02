#nullable enable
using Elffy.AssemblyServices;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Elffy.Diagnostics
{
    public static class DevEnv
    {
        private const string NewLine = "\n";
        private const string DebugSymbol = "DEBUG";

        private static readonly Lazy<Action<string>?> _s_provider_Write = new Lazy<Action<string>?>(GetDebugProvider);

        public static bool IsEnabled { get; private set; }

        [Conditional(DebugSymbol)]
        public static void Run()
        {
            if(IsEnabled) { return; }
            IsEnabled = true;

            // Enable diagnostics
            GCTracker.Init();
        }

        [Conditional(DebugSymbol)]
        public static void Stop()
        {
            IsEnabled = false;

            // Disable diagnostics
            GCTracker.End();
        }

        [Conditional(DebugSymbol)]
        public static void WriteLine(object? value)
        {
            var msg = value?.ToString() + NewLine;
            WritePrivate(msg);
        }

        [Conditional(DebugSymbol)]
        public static void WriteLine(string? value)
        {
            var msg = value + NewLine;
            WritePrivate(msg);
        }

        [Conditional(DebugSymbol)]
        public static void WriteLine(object? value, string? category)
        {
            var msg = category + ": " + value + NewLine;
            WritePrivate(msg);
        }

        [Conditional(DebugSymbol)]
        public static void WriteLine(string format, params object[] args)
        {
            var msg = string.Format(null, format, args) + NewLine;
            WritePrivate(msg);
        }

        [Conditional(DebugSymbol)]
        public static void WriteLine(string? value, string? category)
        {
            var msg = category + ": " + value + NewLine;
            WritePrivate(msg);
        }

        internal static void ForceWriteLine(string? value)
        {
            _s_provider_Write.Value?.Invoke(value + NewLine);
        }

        private static void WritePrivate(string message)
        {
            // This means 'Debug.Write(string)'
            _s_provider_Write.Value?.Invoke(message);
        }

        [CriticalDotnetDependency("netcoreapp3.1")]
        private static Action<string>? GetDebugProvider()
        {
            var s_provider = typeof(Debug).GetField("s_provider", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
            if(s_provider is null == false) {
                return s_provider
                    .GetType()
                    .GetMethod("Write", BindingFlags.Public | BindingFlags.Instance)
                    ?.CreateDelegate(typeof(Action<string>), s_provider) as Action<string>;
            }
            else {
                return null;
            }
        }
    }
}
