#nullable enable
using Elffy.AssemblyServices;
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.Features.Internal
{
    [DontUseDefault]
    internal readonly struct LazyApplyingList<T>
    {
        private readonly List<T> _list;
        private readonly List<(T, Action<T>)> _addedList;
        private readonly List<(T, Action<T>)> _removedList;

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
        public bool ApplyAdd()
        {
            if(_addedList.Count == 0) { return false; }
            Apply(this);
            return true;

            // uncommon path
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Apply(in LazyApplyingList<T> self)
            {
                var addedList = self._addedList;
                int addedCount;
                {
                    var addedListSpan = addedList.AsSpan();
                    addedCount = addedListSpan.Length;
                    var list = self._list;
                    foreach(var (item, onAdded) in addedListSpan) {
                        list.Add(item);
                        onAdded.Invoke(item);
                    }
                }
                addedList.RemoveRange(0, addedCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ApplyRemove()
        {
            if(_removedList.Count == 0) { return false; }
            Apply(this);
            return true;

            // uncommon path
            static void Apply(in LazyApplyingList<T> self)
            {
                var removedList = self._removedList;
                int removedCount;
                {
                    var removedListSpan = removedList.AsSpan();
                    removedCount = removedListSpan.Length;
                    var list = self._list;
                    foreach(var (item, onRemove) in removedListSpan) {
                        list.Remove(item);
                        onRemove.Invoke(item);
                    }
                }
                removedList.RemoveRange(0, removedCount);
            }
        }

        public Span<T> AsSpan() => _list.AsSpan();
        public ReadOnlySpan<T> AsReadOnlySpan() => _list.AsSpan();
    }
}
