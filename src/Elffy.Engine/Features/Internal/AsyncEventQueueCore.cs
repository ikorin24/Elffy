#nullable enable
using Elffy.Effective;
using Elffy.Threading;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy.Features.Internal
{
    internal struct AsyncEventQueueCore
    {
        private WorkItemQueue _queue;
        private FastSpinLock _lock;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Post(Action continuation)
        {
            if(continuation is null) { return; }
            var workItem = new WorkItem(continuation);
            _lock.Enter();
            try {
                _queue.Enqueue(workItem);
            }
            finally {
                _lock.Exit();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Post(Action<object?> continuation, object? state)
        {
            if(continuation is null) { return; }
            var workItem = new WorkItem(continuation, state);
            _lock.Enter();
            try {
                _queue.Enqueue(workItem);
            }
            finally {
                _lock.Exit();
            }
        }

        public void AbortAllEvents()
        {
            _lock.Enter();
            try {
                _queue.Clear();
            }
            finally {
                _lock.Exit();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DoQueuedEvents()
        {
            var count = _queue.Count;
            if(count > 0) {
                Do(ref this, count);

#if !DEBUG
                [DebuggerHidden]
#endif
                static void Do(ref AsyncEventQueueCore self, int count)
                {
                    WorkItem workItem;
                    for(int i = 0; i < count; i++) {
                        try {
                            self._lock.Enter();
                            try {
                                workItem = self._queue.Dequeue();
                            }
                            finally {
                                self._lock.Exit();
                            }
                            workItem.Invoke();
                        }
                        catch {
                            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                            // Don't throw
                        }
                    }
                }
            }
            return;
        }

        private readonly struct WorkItem
        {
            private readonly Action<object?> _action;
            private readonly object? _state;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public WorkItem(Action<object?> action, object? state)
            {
                _action = action;
                _state = state;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public WorkItem(Action action)
            {
                _action = Lambda.Instance.Action;
                _state = action;
            }

#if !DEBUG
            [DebuggerHidden]
#endif
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Invoke() => _action.Invoke(_state);

            private sealed class Lambda
            {
                public static readonly Lambda Instance = new Lambda();
                public readonly Action<object?> Action;
                private Lambda() => Action = new Action<object?>(M);

#if !DEBUG
                [DebuggerHidden]
#endif
                private void M(object? state)
                {
                    Debug.Assert(state is not null);
                    SafeCast.As<Action>(state).Invoke();
                }
            }
        }

        [DebuggerDisplay("{DebuggerView,nq}")]
        [DebuggerTypeProxy(typeof(WorkItemQueueTypeProxy))]
        private struct WorkItemQueue
        {
            private const int MinCapacity = 4;
            private static readonly int ArrayMaxLength =
#if NET6_0
            System.Array.MaxLength
#else
            0X7FFFFFC7
#endif
            ;

            private WorkItem[]? _array;
            private int _head;
            private int _tail;
            private int _count;

            private string DebuggerView => $"{nameof(WorkItem)}[{_count}]";

            public int Count => _count;

            public void Enqueue(WorkItem item)
            {
                if(_array is null) {
                    Debug.Assert(_tail == 0);
                    Debug.Assert(_head == 0);
                    Debug.Assert(_count == 0);
                    _array = new WorkItem[MinCapacity];
                }
                else if(_count == _array.Length) {
                    Grow(ref this);
                    Debug.Assert(_array is not null);
                }
                _array[_tail] = item;
                CycleIncrement(ref _tail, _array.Length);
                _count++;
            }

            public WorkItem Dequeue()
            {
                if(_count == 0) {
                    ThrowEmptyQueue();
                }
                Debug.Assert(_array is not null);
                var result = _array[_head];
                if(RuntimeHelpers.IsReferenceOrContainsReferences<WorkItem>()) {
                    _array[_head] = default;
                }
                CycleIncrement(ref _head, _array.Length);
                _count--;
                return result;
            }

            public WorkItem[] ToArray()
            {
                var array = _array;
                var head = _head;
                var tail = _tail;
                var count = _count;
                if(array is null || array.Length == 0) {
                    return Array.Empty<WorkItem>();
                }
                var copy = new WorkItem[count];
                if(head < tail) {
                    Array.Copy(array, head, copy, 0, count);
                }
                else {
                    Array.Copy(array, head, copy, 0, array.Length - head);
                    Array.Copy(array, 0, copy, array.Length - head, tail);
                }
                return copy;
            }

            public void Clear()
            {
                _count = 0;
                _array = null;
                _tail = 0;
                _head = 0;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void Grow(ref WorkItemQueue self)
            {
                Debug.Assert(self._array is not null);
                var newCapacity = Math.Min(ArrayMaxLength, Math.Max(self._array.Length * 4, MinCapacity));
                var newArray = new WorkItem[newCapacity];
                if(self._count > 0) {
                    if(self._head < self._tail) {
                        Array.Copy(self._array, self._head, newArray, 0, self._count);
                    }
                    else {
                        Array.Copy(self._array, self._head, newArray, 0, self._array.Length - self._head);
                        Array.Copy(self._array, 0, newArray, self._array.Length - self._head, self._tail);
                    }
                }
                self._array = newArray;
                self._head = 0;
                self._tail = (self._count == newCapacity) ? 0 : self._count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void CycleIncrement(ref int index, int cycle)
            {
                int tmp = index + 1;
                if(tmp == cycle) {
                    tmp = 0;
                }
                index = tmp;
            }

            [DoesNotReturn]
            private static void ThrowEmptyQueue() => throw new InvalidOperationException("Queue is empty.");
        }

        private class WorkItemQueueTypeProxy
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly WorkItem[] _items;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public WorkItem[] Items => _items;

            public WorkItemQueueTypeProxy(WorkItemQueue queue)
            {
                _items = queue.ToArray();
            }
        }
    }
}
