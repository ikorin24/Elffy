#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Collections.Generic;

namespace Elffy.Core
{
    internal sealed class LazyApplyingList<T>
    {
        private readonly List<T> _list;
        private readonly List<T> _addedList;
        private readonly List<T> _removedList;

        public int Count => _list.Count;

        public LazyApplyingList()
        {
            _list = new List<T>();
            _addedList = new List<T>();
            _removedList = new List<T>();
        }

        public void Add(in T item)
        {
            _addedList.Add(item);
        }

        public void Remove(in T item)
        {
            _removedList.Add(item);
        }

        public void ApplyAdd()
        {
            if(_addedList.Count == 0) { return; }
            Apply();

            void Apply()
            {
                _list.AddRange(_addedList);
                _addedList.Clear();
            }
        }

        public void ApplyRemove()
        {
            if(_removedList.Count == 0) { return; }
            Apply();

            void Apply()
            {
                foreach(var item in _removedList.AsSpan()) {
                    _list.Remove(item);
                }
                _removedList.Clear();
            }
        }

        public Span<T> AsSpan() => _list.AsSpan();
    }
}
