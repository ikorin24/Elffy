#nullable enable
using Elffy.Core;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.UI
{
    [DebuggerTypeProxy(typeof(ControlCollectionDebuggerTypeProxy))]
    [DebuggerDisplay("{DebugDisplay}")]
    public readonly struct ControlCollection : IEquatable<ControlCollection>
    {
        private readonly Control _owner;

        private string DebugDisplay => $"{nameof(ControlCollection)} (Count = {Count})";

        public Control this[int index] => _owner.ChildrenCore[index];

        public int Count => _owner.ChildrenCore.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ControlCollection(Control owner)
        {
            Debug.Assert(owner is not null);
            _owner = owner;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Control item)
        {
            if(item is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(item));
            }
            if(item.LifeState != LifeState.New) { ThrowNotNewControl(); }
            _owner.ChildrenCore.Add(item);
            item.AddedToListCallback(_owner);
        }

        public unsafe void Clear()
        {
            _owner.ChildrenCore.Clear(&Callback);

            static void Callback(Control[]? items)
            {
                foreach(var item in items.AsSpan()) {
                    if(item is not null) {
                        item.RemovedFromListCallback();
                    }
                    else {
                        return;
                    }
                }
            }
        }

        public bool Contains(Control item)
        {
            return _owner.ChildrenCore.IndexOf(item) >= 0;
        }

        public int IndexOf(Control item)
        {
            return _owner.ChildrenCore.IndexOf(item);
        }

        public bool Remove(Control item)
        {
            if(item is null) {
                return false;
            }
            var result = _owner.ChildrenCore.Remove(item);
            if(result) {
                item.RemovedFromListCallback();
            }
            return result;
        }

        public ReadOnlySpan<Control> AsSpan() => _owner.ChildrenCore.AsSpan();

        public ArraySliceEnumerator<Control> GetEnumerator() => _owner.ChildrenCore.GetEnumerator();

        public override bool Equals(object? obj) => obj is ControlCollection collection && Equals(collection);

        public bool Equals(ControlCollection other) => _owner == other._owner;

        public override int GetHashCode() => _owner is null ? 0 : _owner.GetHashCode();

        public static bool operator ==(ControlCollection left, ControlCollection right) => left.Equals(right);

        public static bool operator !=(ControlCollection left, ControlCollection right) => !(left == right);

        [DoesNotReturn]
        private static void ThrowNotNewControl() => throw new ArgumentException($"{nameof(Control)} object is not new.");
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
