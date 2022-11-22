#nullable enable
using Elffy.AssemblyServices;
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.Features.Internal
{
    [DontUseDefault]
    internal struct LazyApplyingList<T, TOwner>
    {
        private readonly List<T> _list;
        private readonly List<(T, Action<T>)> _addedList;
        private readonly List<(T, Action<T>)> _removedList;
        private EventSource<TOwner> _added;
        private EventSource<TOwner> _removed;

        [UnscopedRef]
        public Event<TOwner> Added => _added.Event;

        [UnscopedRef]
        public Event<TOwner> Removed => _removed.Event;

        public int Count => _list.Count;

        public LazyApplyingList()
        {
            _list = new List<T>();
            _addedList = new();
            _removedList = new();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item, Action<T> onAdded)
        {
            _addedList.Add((item, onAdded));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(in T item, Action<T> onRemoved)
        {
            _removedList.Add((item, onRemoved));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _list.Clear();
            _addedList.Clear();
            _removedList.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ApplyAdd(TOwner owner)
        {
            if(_addedList.Count == 0) { return false; }
            ApplyAddPrivate(owner);
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ApplyAddPrivate(TOwner owner)
        {
            var addedList = _addedList;
            int addedCount;
            {
                var addedListSpan = addedList.AsSpan();
                addedCount = addedListSpan.Length;
                var list = _list;
                foreach(var (item, onAdded) in addedListSpan) {
                    list.Add(item);
                    onAdded.Invoke(item);
                }
            }
            addedList.RemoveRange(0, addedCount);
            _added.InvokeIgnoreException(owner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ApplyRemove(TOwner owner)
        {
            if(_removedList.Count == 0) { return false; }
            ApplyRemovePrivate(owner);
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ApplyRemovePrivate(TOwner owner)
        {
            var removedList = _removedList;
            int removedCount;
            {
                var removedListSpan = removedList.AsSpan();
                removedCount = removedListSpan.Length;
                var list = _list;
                foreach(var (item, onRemove) in removedListSpan) {
                    list.Remove(item);
                    onRemove.Invoke(item);
                }
            }
            removedList.RemoveRange(0, removedCount);
            _removed.InvokeIgnoreException(owner);
        }

        public void Sort(Comparison<T> comparison) => _list.AsSpan().Sort(comparison);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan() => _list.AsSpan();
    }
}
