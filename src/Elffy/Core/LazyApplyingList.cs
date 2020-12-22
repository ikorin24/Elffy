#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.Core
{
    internal readonly struct LazyApplyingList<T>
    {
        private readonly List<T> _list;
        private readonly List<T> _addedList;
        private readonly List<T> _removedList;

        public int Count => _list.Count;

        private LazyApplyingList(int dummyArg)
        {
            _list = new List<T>();
            _addedList = new List<T>();
            _removedList = new List<T>();
        }

        public static LazyApplyingList<T> New()
        {
            return new LazyApplyingList<T>(0);
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
        public void ApplyAdd()
        {
            if(_addedList.Count == 0) { return; }
            Apply(this);

            // uncommon path
            static void Apply(in LazyApplyingList<T> self)
            {
                self._list.AddRange(self._addedList);
                self._addedList.Clear();
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

        public ReadOnlySpan<T> AsSpan() => _list.AsSpan();
    }
}
