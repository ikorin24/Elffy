#nullable enable
using Elffy.Features;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    [DebuggerTypeProxy(typeof(PositionableCollectionDebuggerTypeProxy))]
    [DebuggerDisplay("{DebugDisplay}")]
    public readonly struct PositionableCollection : IEquatable<PositionableCollection>
    {
        private readonly Positionable _owner;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay => $"{nameof(PositionableCollection)} (Count = {Count})";

        public Positionable this[int index] => _owner.ChildrenCore[index];

        public int Count => _owner.ChildrenCore.Count;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public PositionableCollection() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PositionableCollection(Positionable owner)
        {
            Debug.Assert(owner is not null);
            _owner = owner;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Positionable item)
        {
            ThrowIfInvalidInstance();
            ArgumentNullException.ThrowIfNull(item);
            if(item.Parent != null) { ThrowAlreadyHasParent(); }
            _owner.ChildrenCore.Add(item);
            item.Parent = _owner;
        }

        public bool Contains(Positionable item)
        {
            ThrowIfInvalidInstance();
            ArgumentNullException.ThrowIfNull(item);
            return _owner.ChildrenCore.IndexOf(item) >= 0;
        }

        public int IndexOf(Positionable item)
        {
            ThrowIfInvalidInstance();
            ArgumentNullException.ThrowIfNull(item);
            return _owner.ChildrenCore.IndexOf(item);
        }

        public void Insert(int index, Positionable item)
        {
            ThrowIfInvalidInstance();
            if((uint)index > (uint)Count) {
                ThrowOutOfRange();
                [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
            }
            ArgumentNullException.ThrowIfNull(item);
            if(item.Parent != null) { ThrowAlreadyHasParent(); }
            _owner.ChildrenCore.Insert(index, item);
            item.Parent = _owner;
        }

        public bool Remove(Positionable item)
        {
            ThrowIfInvalidInstance();
            ArgumentNullException.ThrowIfNull(item);
            var result = _owner.ChildrenCore.Remove(item);
            if(result) {
                item.Parent = null;
            }
            return result;
        }

        public void RemoveAt(int index)
        {
            ThrowIfInvalidInstance();
            ref var core = ref _owner.ChildrenCore;
            var item = core[index];
            core.RemoveAt(index);
            item.Parent = null;
        }

        public Positionable[] ToArray() => _owner.ChildrenCore.AsSpan().ToArray();

        public ReadOnlySpan<Positionable> AsSpan() => _owner.ChildrenCore.AsSpan();

        public ArraySliceEnumerator<Positionable> GetEnumerator() => _owner.ChildrenCore.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfInvalidInstance()
        {
            if(_owner is null || _owner.LifeState == LifeState.Dead) {
                Throw();
                [DoesNotReturn] static void Throw() => throw new InvalidOperationException($"Parent is already dead or the {nameof(PositionableCollection)} is invalid.");
            }
        }

        [DoesNotReturn]
        private static void ThrowAlreadyHasParent() => throw new InvalidOperationException($"The instance is already a child of another object.");

        public override string ToString() => nameof(PositionableCollection);

        public override bool Equals(object? obj) => obj is PositionableCollection collection && Equals(collection);

        public bool Equals(PositionableCollection other) => ReferenceEquals(_owner, other._owner);

        public override int GetHashCode() => _owner.GetHashCode();

        public static bool operator ==(PositionableCollection left, PositionableCollection right) => left.Equals(right);

        public static bool operator !=(PositionableCollection left, PositionableCollection right) => !(left == right);
    }

    internal sealed class PositionableCollectionDebuggerTypeProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PositionableCollection _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Positionable[] Items => _entity.ToArray();

        public PositionableCollectionDebuggerTypeProxy(PositionableCollection entity) => _entity = entity;
    }
}
