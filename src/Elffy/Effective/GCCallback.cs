#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Elffy.AssemblyServices;

namespace Elffy.Effective
{
    /// <summary>GC callback class</summary>
    internal static class GCCallback
    {
        /// <summary>Register callback of GC in specified generation. Callback is alive while it returns true.</summary>
        /// <remarks>
        /// <paramref name="generation"/> is min gen where callback fires.
        /// For example, if regester to gen2, callback fires on gen0, gen1 and gen2.
        /// </remarks>
        /// <param name="callback">callback method which fires after GC</param>
        /// <param name="generation">target generation of GC</param>
        public static void Register(Func<bool> callback, int generation)
        {
            Debug.Assert(callback is null == false);

            // This is zombie object. No one can access it but it survives.
            new CallbackHolder(callback, Math.Min(generation, GC.MaxGeneration));
        }

        /// <summary>Register callback of GC in specified generation. Callback is alive while it returns true, or argument of it is alive.</summary>
        /// <remarks>
        /// <paramref name="generation"/> is min gen where callback fires.
        /// For example, if regester to gen2, callback fires on gen0, gen1 and gen2.
        /// </remarks>
        /// <param name="callback">callback method which fires after GC</param>
        /// <param name="callbackArg">argument of callback method</param>
        /// <param name="generation">target generation of GC</param>
        public static void Register(Func<object, bool> callback, object callbackArg, int generation)
        {
            Debug.Assert(callback is null == false);
            Debug.Assert(callbackArg is null == false);

            // This is zombie object. No one can access it but it survives.
            new CallbackWithArgHolder(callback, callbackArg, Math.Min(generation, GC.MaxGeneration));
        }

        private class CallbackHolder
        {
            private readonly Func<bool> _callback;
            private readonly int _targetGen;
            private int _targetGenCount;

            public CallbackHolder(Func<bool> callback, int targetGen)
            {
                _callback = callback;
                _targetGen = targetGen;
                _targetGenCount = GC.CollectionCount(targetGen);
            }

            ~CallbackHolder()
            {
                // Keep this object alive by `GC.ReRegisterForFinalize(this)`
                // until the callback returns false.

                var targetGenCount = GC.CollectionCount(_targetGen);
                if(targetGenCount != _targetGenCount) {
                    _targetGenCount = targetGenCount;
                    try {
                        if(!_callback()) { return; }
                    }
                    catch {
                        if(AssemblyState.IsDebug) { throw; }
                    }
                }
                GC.ReRegisterForFinalize(this);
            }
        }

        private class CallbackWithArgHolder
        {
            private readonly Func<object, bool> _callback;
            private readonly GCHandle _callbackArg;
            private readonly int _targetGen;
            private int _targetGenCount;

            public CallbackWithArgHolder(Func<object, bool> callback, object callbackArg, int targetGen)
            {
                _callback = callback;
                _callbackArg = GCHandle.Alloc(callbackArg, GCHandleType.Weak);
                _targetGen = targetGen;
                _targetGenCount = GC.CollectionCount(targetGen);
            }

            ~CallbackWithArgHolder()
            {
                // Keep this object alive by `GC.ReRegisterForFinalize(this)`
                // until the callback returns false or arg is alive.

                var targetGenCount = GC.CollectionCount(_targetGen);
                if(targetGenCount != _targetGenCount) {
                    try {
                        _targetGenCount = targetGenCount;
                        var arg = _callbackArg.Target;
                        if(arg is null) {
                            _callbackArg.Free();
                            return;
                        }
                        if(!_callback(arg)) { return; }
                    }
                    catch {
                        if(AssemblyState.IsDebug) { throw; }
                    }
                }
                GC.ReRegisterForFinalize(this);
            }
        }
    }
}
