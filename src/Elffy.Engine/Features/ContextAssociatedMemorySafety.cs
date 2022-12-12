#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;
using Elffy.Effective;
using Elffy.Diagnostics;

namespace Elffy.Features
{
    /// <summary>Safety class for leaks of OpenGL-context-associated memory</summary>
    public static class ContextAssociatedMemorySafety
    {
        private static readonly ConditionalWeakTable<IDisposable, IHostScreen> _dict = new();
        private static readonly Dictionary<IHostScreen, List<IDisposable>> _waitingDisposing = new();

        /// <summary>Get the safety is enabled.</summary>
        /// <remarks>If you want to set the value, use <see cref="EngineSetting.EnableContextAssociatedMemorySafety"/>.</remarks>
        public static bool IsEnabled => EngineSetting.EnableContextAssociatedMemorySafety;

        public static SafetyKey RegisterNonDisposable<T>(T resource, Action<T> disposeAction, IHostScreen associatedScreen) where T : class
        {
            if(!IsEnabled) { return SafetyKey.Empty; }
            var d = new DisposableWrapper<T>(resource, disposeAction);
            _dict.Add(d, associatedScreen);
            return new SafetyKey(d);
        }

        /// <summary>Register a resource</summary>
        /// <typeparam name="T">type of resource</typeparam>
        /// <param name="resource">resource object</param>
        /// <param name="associatedScreen">associated screen which has opengl context</param>
        public static void Register<T>(T resource, IHostScreen associatedScreen) where T : class, IDisposable
        {
            if(!IsEnabled) { return; }
            _dict.Add(resource, associatedScreen);
        }

        public static void OnFinalized(SafetyKey safetyKey)
        {
            var disposable = safetyKey.Disposable;
            if(disposable != null) {
                OnFinalized(disposable);
            }
        }

        /// <summary>Add specified <paramref name="resource"/> to the queue for safety disposing.</summary>
        /// <remarks>This method must be called from finalizer of the <paramref name="resource"/>.</remarks>
        /// <typeparam name="T">type of resource</typeparam>
        /// <param name="resource">target resource</param>
        public static void OnFinalized<T>(T resource) where T : class, IDisposable
        {
            // This method must be called from finalizer of the resource.

            if(!IsEnabled) { return; }

            if(_dict.TryGetValue(resource, out var screen)) {
                _dict.Remove(resource);
                if(_waitingDisposing.TryGetValue(screen, out var list)) {
                    list.Add(resource);
                }
                else {
                    list = new();
                    list.Add(resource);
                    _waitingDisposing.Add(screen, list);
                }
            }
        }

        internal static void EnsureCollect(IHostScreen targetScreen)
        {
            Debug.Assert(Engine.CurrentContext == targetScreen);
            if(!IsEnabled) { return; }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            CollectIfExist(targetScreen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CollectIfExist(IHostScreen targetScreen)
        {
            // This method is called every frame

            Debug.Assert(Engine.CurrentContext == targetScreen);
            if(!IsEnabled) { return; }
            Collect(targetScreen);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Collect(IHostScreen targetScreen)
            {
                if(_waitingDisposing.TryGetValue(targetScreen, out var list) && list.Count > 0) {
                    var items = list.ClearWithExtracting();
                    DevEnv.ForceWriteLine("Some resources are leaked! Dispose them for safety.", nameof(ContextAssociatedMemorySafety));
#if DEBUG
                    // ===========================================================
                    // If you are looking at this comment,
                    // you may have forgotten to call `Dispose()` of some object.
                    // ===========================================================
                    Debugger.Break();
#endif
                    foreach(var item in items) {
                        item.Dispose();
                        if(DevEnv.IsEnabled) {
                            DevEnv.ForceWriteLine($"'{item.GetType().FullName}' is leaked.", nameof(ContextAssociatedMemorySafety));
                        }
                    }
                }
            }
        }

        private sealed class DisposableWrapper<T> : IDisposable where T : class
        {
            private readonly T _obj;
            private readonly Action<T> _disposeAction;

            public DisposableWrapper(T obj, Action<T> disposeAction)
            {
                _obj = obj;
                _disposeAction = disposeAction;
            }

            public void Dispose() => _disposeAction.Invoke(_obj);
        }

        public readonly struct SafetyKey : IEquatable<SafetyKey>
        {
            private readonly IDisposable? _disposable;

            internal static SafetyKey Empty => default;

            internal IDisposable? Disposable => _disposable;

            internal SafetyKey(IDisposable disposable) => _disposable = disposable;

            public override bool Equals(object? obj) => obj is SafetyKey @object && Equals(@object);

            public bool Equals(SafetyKey other) => _disposable == other._disposable;

            public override int GetHashCode() => _disposable?.GetHashCode() ?? 0;
        }
    }
}
