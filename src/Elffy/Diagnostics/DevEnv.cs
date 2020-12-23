#nullable enable
using Elffy.AssemblyServices;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Elffy.Diagnostics
{
    public static class DevEnv
    {
        private const string NewLine = "\n";
        private const string DebugSymbol = "DEBUG";

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
            WritePrivate(value.ToString() + NewLine, category);
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
            WritePrivate(value + NewLine, category);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ForceWriteLine(string? value)
        {
            if(IsEnabled) {
                WritePrivate(value + NewLine);
            }
        }

        private static void WritePrivate(string message, string? category = null)
        {
            Debugger.Log(0, category, message);
        }
    }
}
