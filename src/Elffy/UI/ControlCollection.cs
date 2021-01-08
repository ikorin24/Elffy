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
    public readonly struct ControlCollection
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
                if(items is not null) {
                    foreach(var item in items) {
                        item.RemovedFromListCallback();
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

        public void Insert(int index, Control item)
        {
            if(item is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(item));
            }

            if(item.LifeState == LifeState.New) { ThrowNotNewControl(); }
            _owner.ChildrenCore.Insert(index, item);
            item.AddedToListCallback(_owner);
        }

        public bool Remove(Control item)
        {
            if(item is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(item));
            }
            var result = _owner.ChildrenCore.Remove(item);
            if(result) {
                item.RemovedFromListCallback();
            }
            return result;
        }

        [DoesNotReturn]
        private static void ThrowNotNewControl()
        {
            throw new ArgumentException($"{nameof(Control)} object is not new.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfInvalidInstance()
        {
            if(_owner is null || _owner.LifeState == LifeState.Dead) {
                Throw();
                [DoesNotReturn] static void Throw() => throw new InvalidOperationException($"Parent is already dead or the {nameof(ControlCollection)} is invalid.");
            }
        }

        public Control[] ToArray() => _owner.ChildrenCore.AsSpan().ToArray();

        public ReadOnlySpan<Control> AsSpan() => _owner.ChildrenCore.AsSpan();

        public ArraySliceEnumerator<Control> GetEnumerator() => _owner.ChildrenCore.GetEnumerator();
    }

    internal sealed class ControlCollectionDebuggerTypeProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ControlCollection _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Control[] Items => _entity.ToArray();

        public ControlCollectionDebuggerTypeProxy(ControlCollection entity) => _entity = entity;
    }
}
