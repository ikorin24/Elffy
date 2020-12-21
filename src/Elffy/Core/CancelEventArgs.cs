#nullable enable
using System;

namespace Elffy.Core
{
    /// <summary>Event argument struct for events which can get canceled.</summary>
    public readonly ref struct CancelEventArgs
    {
        private readonly IntPtr _cancel;    // bool*

        /// <summary>Get or set the event is canceled</summary>
        public unsafe bool Cancel
        {
            get => _cancel == IntPtr.Zero ? false : *(bool*)_cancel;
            set
            {
                if(_cancel != IntPtr.Zero) {
                    *(bool*)_cancel = value;
                }
            }
        }

        /// <summary>Create <see cref="CancelEventArgs"/> with reference to cancel flag.</summary>
        /// <param name="cancel">pointer to cancel flag, which must be pinned. (It is usually pointer to stack memory.)</param>
        public unsafe CancelEventArgs(bool* cancel)
        {
            if(cancel == null) { ThrowNullArg(); }
            _cancel = (IntPtr)cancel;

            static void ThrowNullArg() => throw new ArgumentNullException(nameof(cancel));
        }
    }

    public delegate void ClosingEventHandler<T>(T sender, CancelEventArgs e);
}
