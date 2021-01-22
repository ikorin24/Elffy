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
            if(item.LifeState != ControlLifeState.New) { ThrowNotNewControl(); }
            var index = _owner.ChildrenCore.Count;
            _owner.ChildrenCore.Add(item);
            item.AddedToListCallback(_owner, index);
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

        [DoesNotReturn]
        private static void ThrowNotNewControl()
        {
            throw new ArgumentException($"{nameof(Control)} object is not new.");
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
