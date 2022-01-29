#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Features;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.UI
{
    [DebuggerTypeProxy(typeof(ControlCollectionDebuggerTypeProxy))]
    [DebuggerDisplay("{DebugDisplay}")]
    public readonly struct ControlCollection : IEquatable<ControlCollection>
    {
        private readonly Control _owner;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay => $"{nameof(ControlCollection)} (Count = {Count})";

        public Control this[int index] => _owner.ChildrenCore[index];

        public int Count => _owner.ChildrenCore.Count;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ControlCollection() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ControlCollection(Control owner)
        {
            Debug.Assert(owner is not null);
            _owner = owner;
        }

        public UniTask Add(Control item) => _owner.AddChild(item);

        public UniTask Remove(Control item) => _owner.RemoveChild(item);

        public UniTask Clear() => _owner.ClearChildren();

        public bool Contains(Control item) => _owner.ChildrenCore.IndexOf(item) >= 0;

        public int IndexOf(Control item) => _owner.ChildrenCore.IndexOf(item);

        public Control[] ToArray() => AsSpan().ToArray();

        public ReadOnlySpan<Control> AsSpan() => _owner.ChildrenCore.AsSpan();

        public ArraySliceEnumerator<Control> GetEnumerator() => _owner.ChildrenCore.GetEnumerator();

        public override bool Equals(object? obj) => obj is ControlCollection collection && Equals(collection);

        public bool Equals(ControlCollection other) => _owner == other._owner;

        public override int GetHashCode() => _owner is null ? 0 : _owner.GetHashCode();

        public static bool operator ==(ControlCollection left, ControlCollection right) => left.Equals(right);

        public static bool operator !=(ControlCollection left, ControlCollection right) => !(left == right);
    }

    internal sealed class ControlCollectionDebuggerTypeProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ControlCollection _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Control[] Items => _entity.AsSpan().ToArray();

        public ControlCollectionDebuggerTypeProxy(ControlCollection entity) => _entity = entity;
    }
}
