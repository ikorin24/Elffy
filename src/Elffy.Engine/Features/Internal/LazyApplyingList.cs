#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.Features.Internal
{
    internal readonly struct LazyApplyingList<T>
    {
        private readonly List<T> _list;
        private readonly List<T> _addedList;
        private readonly List<T> _removedList;

        public int Count => _list.Count;

        private LazyApplyingList(List<T> list, List<T> addedList, List<T> removedList)
        {
            _list = list;
            _addedList = addedList;
            _removedList = removedList;
        }

        public static LazyApplyingList<T> New()
        {
            return new LazyApplyingList<T>(new List<T>(), new List<T>(), new List<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item)
        {
            _addedList.Add(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(in T item)
        {
            _removedList.Add(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _list.Clear();
            _addedList.Clear();
            _removedList.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyAdd()
        {
            if(_addedList.Count == 0) { return; }
            Apply(this);

            // uncommon path
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Apply(in LazyApplyingList<T> self)
            {
                self._list.AddRange(self._addedList);
                self._addedList.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyAdd(Action<T> onAdded)
        {
            if(_addedList.Count == 0) { return; }
            Apply(this, onAdded);

            // uncommon path
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Apply(in LazyApplyingList<T> self, Action<T> onAdded)
            {
                var addedList = self._addedList;
                var list = self._list;
                foreach(var item in addedList.AsSpan()) {
                    list.Add(item);
                    onAdded(item);
                }
                addedList.Clear();
            }
        }

        public void ApplyRemove(Action<T> onRemoved)
        {
            if(_removedList.Count == 0) { return; }

            Apply(this, onRemoved);

            // uncommon path
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Apply(in LazyApplyingList<T> self, Action<T> onRemoved)
            {
                foreach(var item in self._removedList.AsSpan()) {
                    if(self._list.Remove(item)) {
                        onRemoved.Invoke(item);
                    }
                }
                self._removedList.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyRemove()
        {
            if(_removedList.Count == 0) { return; }
            Apply(this);

            // uncommon path
            static void Apply(in LazyApplyingList<T> self)
            {
                foreach(var item in self._removedList.AsSpan()) {
                    self._list.Remove(item);
                }
                self._removedList.Clear();
            }
        }

        public Span<T> AsSpan() => _list.AsSpan();
        public ReadOnlySpan<T> AsReadOnlySpan() => _list.AsSpan();
    }
}
