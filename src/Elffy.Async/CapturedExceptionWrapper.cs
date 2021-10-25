#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    internal readonly struct CapturedExceptionWrapper
    {
        private readonly object? _exception;

        public static CapturedExceptionWrapper None => default;

        public UniTaskCapturedExceptionStatus Status
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var ex = _exception;
                if(ex is null) {
                    return UniTaskCapturedExceptionStatus.None;
                }
                else if(ex is OperationCanceledException) {
                    return UniTaskCapturedExceptionStatus.Canceled;
                }
                else {
                    return UniTaskCapturedExceptionStatus.Faulted;
                }
            }
        }

        private CapturedExceptionWrapper(OperationCanceledException oce)
        {
            _exception = oce;
        }

        private CapturedExceptionWrapper(ExceptionHolder holder)
        {
            _exception = holder;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static CapturedExceptionWrapper Capture(Exception ex)
        {
            Debug.Assert(ex is not null);
            if(ex is OperationCanceledException oce) {
                return new CapturedExceptionWrapper(oce);
            }
            else {
                return new CapturedExceptionWrapper(ExceptionHolder.Capture(ex));
            }
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ThrowIfCaptured()
        {
            var ex = _exception;
            if(ex is null) { return; }
            ThrowCore(ex);
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        private static void ThrowCore(object ex)
        {
            if(ex is OperationCanceledException oce) {
                throw oce;
            }
            else if(ex is ExceptionHolder holder) {
                holder.Throw();
            }
            else {
                throw new InvalidOperationException($"Critical: Invalid type exception captured !!!");
            }
        }
    }

    internal enum UniTaskCapturedExceptionStatus : byte
    {
        None = 0,
        Canceled = 1,
        Faulted = 2,
    }
}
