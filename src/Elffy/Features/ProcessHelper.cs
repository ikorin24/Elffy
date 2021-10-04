#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;

namespace Elffy.Features
{
    /// <summary>Provides utilities of the process</summary>
    public static class ProcessHelper
    {
        /// <summary>Max length of a mutex name</summary>
        private const int MutexNameMaxLength = 259;

        private static readonly Lazy<string> _defaultUniqueName = new(() =>
        {
            var assemblyName = Assembly.GetEntryAssembly()!.GetName();
            var uniqueName = $"{assemblyName.Name}-{assemblyName.Version}";      // at least one character
            if(uniqueName.Length > MutexNameMaxLength) {
                uniqueName = uniqueName.Substring(0, MutexNameMaxLength);
            }
            return uniqueName;

        }, LazyThreadSafetyMode.PublicationOnly);

        /// <summary>Use mutex to prevent multiple launches of applications</summary>
        /// <param name="startAction">action to launch</param>
        /// <param name="multiLaunch">action executed at multiple startup</param>
        public static void SingleLaunch(Action startAction, Action? multiLaunch = null)
        {
            SingleLaunch(_defaultUniqueName.Value, startAction, multiLaunch);
        }

        /// <summary>Use mutex to prevent multiple launches of applications</summary>
        /// <param name="uniqueName">mutex name (It must be unique that represents the application.)</param>
        /// <param name="startAction">action to launch</param>
        /// <param name="multiLaunch">action executed at multiple startup</param>
        public static void SingleLaunch(string uniqueName, Action startAction, Action? multiLaunch = null)
        {
            if(string.IsNullOrEmpty(uniqueName)) { ThrowNullOrEmpty(nameof(uniqueName)); }
            if(startAction is null) { ThrowNullArg(nameof(startAction)); }
            if(uniqueName.Length > MutexNameMaxLength) { ThrowTooLong(nameof(uniqueName)); }

            using var mutex = new Mutex(true, uniqueName, out var createdNew);
            if(createdNew) {
                startAction();
            }
            else {
                multiLaunch?.Invoke();
            }

            [DoesNotReturn] static void ThrowNullArg(string name) => throw new ArgumentNullException(name);
            [DoesNotReturn] static void ThrowNullOrEmpty(string name) => throw new ArgumentException($"{name} is null or empty.");
            [DoesNotReturn] static void ThrowTooLong(string name) => throw new ArgumentException($"{name} is too long. Length must be between 0 and {MutexNameMaxLength}.");
        }

        /// <summary>Use mutex to prevent multiple launches of applications</summary>
        /// <typeparam name="T">type of <paramref name="arg"/></typeparam>
        /// <param name="startAction">action to launch</param>
        /// <param name="arg">argument of <paramref name="startAction"/></param>
        public static void SingleLaunch<T>(LaunchDelegate<T> startAction, in T arg)
        {
            SingleLaunch<T, int>(_defaultUniqueName.Value, startAction, arg, null, default);
        }

        /// <summary>Use mutex to prevent multiple launches of applications</summary>
        /// <typeparam name="T1">type of <paramref name="arg1"/></typeparam>
        /// <typeparam name="T2">type of <paramref name="arg2"/></typeparam>
        /// <param name="startAction">action to launch</param>
        /// <param name="arg1">argument of <paramref name="startAction"/></param>
        /// <param name="multiLaunch">action executed at multiple startup</param>
        /// <param name="arg2">argument of <paramref name="multiLaunch"/></param>
        public static void SingleLaunch<T1, T2>(LaunchDelegate<T1> startAction, in T1 arg1, LaunchDelegate<T2>? multiLaunch, in T2 arg2)
        {
            SingleLaunch(_defaultUniqueName.Value, startAction, arg1, multiLaunch, arg2);
        }

        /// <summary>Use mutex to prevent multiple launches of applications</summary>
        /// <typeparam name="T1">type of <paramref name="arg1"/></typeparam>
        /// <typeparam name="T2">type of <paramref name="arg2"/></typeparam>
        /// <param name="uniqueName">mutex name (It must be unique that represents the application.)</param>
        /// <param name="startAction">action to launch</param>
        /// <param name="arg1">argument of <paramref name="startAction"/></param>
        /// <param name="multiLaunch">action executed at multiple startup</param>
        /// <param name="arg2">argument of <paramref name="multiLaunch"/></param>
        public static void SingleLaunch<T1, T2>(string uniqueName, LaunchDelegate<T1> startAction, in T1 arg1, LaunchDelegate<T2>? multiLaunch, in T2 arg2)
        {
            if(string.IsNullOrEmpty(uniqueName)) { ThrowNullOrEmpty(nameof(uniqueName)); }
            if(startAction is null) { ThrowNullArg(nameof(startAction)); }
            if(uniqueName.Length > MutexNameMaxLength) { ThrowTooLong(nameof(uniqueName)); }

            using var mutex = new Mutex(true, uniqueName, out var createdNew);
            if(createdNew) {
                startAction(arg1);
            }
            else {
                multiLaunch?.Invoke(arg2!);
            }

            [DoesNotReturn] static void ThrowNullArg(string name) => throw new ArgumentNullException(name);
            [DoesNotReturn] static void ThrowNullOrEmpty(string name) => throw new ArgumentException($"{name} is null or empty.");
            [DoesNotReturn] static void ThrowTooLong(string name) => throw new ArgumentException($"{name} is too long. Length must be between 0 and {MutexNameMaxLength}.");
        }

        public delegate void LaunchDelegate<T>(in T arg);
    }
}
