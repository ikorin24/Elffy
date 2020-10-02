#nullable enable
using System;

namespace Elffy.OpenGL.Windowing
{
    /// <summary>Event argument struct for events which can get canceled.</summary>
    internal readonly ref struct CancelEventArgs
    {
        private readonly IntPtr _cancel;

        /// <summary>Get or set the event is canceled</summary>
        public unsafe bool Cancel
        {
            get => *(bool*)_cancel;
            set => *(bool*)_cancel = value;
        }

        /// <summary>Create <see cref="CancelEventArgs"/> with reference to cancel flag.</summary>
        /// <param name="cancel">pointer to cancel flag, which must be pinned. (It is usually pointer to stack memory.)</param>
        internal unsafe CancelEventArgs(bool* cancel)
        {
            if(cancel == null) { ThrowNullArg(); }
            _cancel = (IntPtr)cancel;

            static void ThrowNullArg() => throw new ArgumentNullException(nameof(cancel));
        }
    }
}
