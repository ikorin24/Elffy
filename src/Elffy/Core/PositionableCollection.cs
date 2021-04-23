#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.Core
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
            if(item is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(item));
            }
            item.Parent = _owner;
            _owner.ChildrenCore.Add(item);
        }

        public void Clear()
        {
            ThrowIfInvalidInstance();
            ref var core = ref _owner.ChildrenCore;
            foreach(var item in core.AsSpan()) {
                item.Parent = null;
            }
            core.Clear();
        }

        public bool Contains(Positionable item)
        {
            ThrowIfInvalidInstance();
            return _owner.ChildrenCore.IndexOf(item) >= 0;
        }

        public int IndexOf(Positionable item)
        {
            ThrowIfInvalidInstance();
            return _owner.ChildrenCore.IndexOf(item);
        }

        public void Insert(int index, Positionable item)
        {
            ThrowIfInvalidInstance();
            if(item is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(item));
            }

            item.Parent = _owner;
            _owner.ChildrenCore.Insert(index, item);
        }

        public bool Remove(Positionable item)
        {
            ThrowIfInvalidInstance();
            if(item is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(item));
            }
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
            core[index].Parent = null;
            core.RemoveAt(index);
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
