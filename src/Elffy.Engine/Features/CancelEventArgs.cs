#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Features
{
    /// <summary>Event argument struct for events which can get canceled.</summary>
    public readonly ref struct CancelEventArgs
    {
        private readonly ref bool _canceled;

        public bool Canceled { get => _canceled; set => _canceled = value; }

        public CancelEventArgs([UnscopedRef] out bool canceled)
        {
            canceled = false;
            _canceled = ref canceled;
        }
    }

    public delegate void ClosingEventHandler<T>(T sender, CancelEventArgs e);
}
