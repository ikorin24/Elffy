#nullable enable
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Elffy.Effective
{
    [DebuggerDisplay("{DebuggerView,nq}")]
    public struct FastSpinLock
    {
        private const int SYNC_ENTER = 1;
        private const int SYNC_EXIT = 0;

        private int _syncFlag;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerView => _syncFlag == SYNC_EXIT ? "unlocked" : "locked";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enter()
        {
            if(Interlocked.CompareExchange(ref _syncFlag, SYNC_ENTER, SYNC_EXIT) == SYNC_ENTER) {
                SpinWait();
            }
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter()
        {
            return Interlocked.CompareExchange(ref _syncFlag, SYNC_ENTER, SYNC_EXIT) == SYNC_EXIT;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit()
        {
            Volatile.Write(ref _syncFlag, SYNC_EXIT);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SpinWait()
        {
            var spinner = new SpinWait();
            spinner.SpinOnce();
            while(Interlocked.CompareExchange(ref _syncFlag, SYNC_ENTER, SYNC_EXIT) == SYNC_ENTER) {
                spinner.SpinOnce();
            }
        }
    }
}
