#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Elffy
{
    internal sealed class ExceptionHolder
    {
        private ExceptionDispatchInfo? _info;

        private ExceptionHolder(ExceptionDispatchInfo info)
        {
            Debug.Assert(info is not null);
            _info = info;
        }

        [DebuggerHidden]
        public static ExceptionHolder Capture(Exception ex)
        {
            return new ExceptionHolder(ExceptionDispatchInfo.Capture(ex));
        }

        [DebuggerHidden]
        [DoesNotReturn]
        public void Throw()
        {
            var info = Interlocked.Exchange(ref _info, null);
            if(info != null) {
                GC.SuppressFinalize(this);
                info.Throw();
            }
            else {
                throw new InvalidOperationException($"Critical: Can not call {nameof(ExceptionHolder)}.{nameof(Throw)} twice !!!");
            }
        }

        ~ExceptionHolder()
        {
            // TODO: Report the unhandled exception.
        }
    }
}
