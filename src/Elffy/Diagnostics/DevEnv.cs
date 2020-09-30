#nullable enable
using Elffy.AssemblyServices;
using System;
using System.Threading;
using System.Diagnostics;

namespace Elffy.Diagnostics
{
    public static class DevEnv
    {
        private static IDevEnvProvider _provider = new DefaultDevEnvProvider();

        [Conditional(AssemblyState.Symbol_Develop)]
        public static void SetProvider(IDevEnvProvider provider)
        {
            Interlocked.Exchange(ref _provider, provider);
        }

        [Conditional(AssemblyState.Symbol_Develop)]
        public static void WriteLine(object? value)
        {
            var msg = value?.ToString() + Environment.NewLine;
            WritePrivate(0, null, msg);
        }

        [Conditional(AssemblyState.Symbol_Develop)]
        public static void WriteLine(string? value)
        {
            var msg = value + Environment.NewLine;
            WritePrivate(0, null, msg);
        }

        [Conditional(AssemblyState.Symbol_Develop)]
        public static void WriteLine(object? value, string? category)
        {
            var msg = value + Environment.NewLine;
            WritePrivate(0, category, msg);
        }

        [Conditional(AssemblyState.Symbol_Develop)]
        public static void WriteLine(string format, params object[] args)
        {
            var msg = string.Format(null, format, args) + Environment.NewLine;
            WritePrivate(0, null, msg);
        }

        [Conditional(AssemblyState.Symbol_Develop)]
        public static void WriteLine(string? value, string? category)
        {
            var msg = value + Environment.NewLine;
            WritePrivate(0, category, msg);
        }

        private static void WritePrivate(int level, string? category, string msg)
        {
            Debugger.Log(level, category, msg);
        }

        private sealed class DefaultDevEnvProvider : IDevEnvProvider
        {
            public void Write(string? category, string message)
            {
                Debugger.Log(0, category, message);
            }
        }
    }
}
