#nullable enable
using Elffy.Effective;
using Elffy.Threading;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    public struct EventSource<T> : IEquatable<EventSource<T>>
    {
        private EventHandlerHolder<T>? _source;

        public static EventSource<T> Default => default;

        [UnscopedRef]
        public Event<T> Event => new Event<T>(ref _source);

        public readonly int SubscibedCount => _source?.SubscibedCount ?? 0;

        public readonly void Invoke(T arg) => _source?.Invoke(arg);

        public readonly void Clear() => _source?.Clear();

        public readonly Action<T> ToDelegate() => new Action<T>(Invoke);

        public readonly override bool Equals(object? obj) => obj is EventSource<T> wrap && Equals(wrap);

        public readonly bool Equals(EventSource<T> other) => _source == other._source;

        public readonly override int GetHashCode() => _source?.GetHashCode() ?? 0;

        public static bool operator ==(EventSource<T> left, EventSource<T> right) => left.Equals(right);

        public static bool operator !=(EventSource<T> left, EventSource<T> right) => !(left == right);
    }

    internal sealed class EventHandlerHolder<T>
    {
        private const int DefaultBufferCapacity = ArrayPoolForEventSource.LengthOfPoolTargetArray;

        // [NOTE]
        // _count == 0      || 1         || n (n >= 2)
        // _actions == null || Action<T> || object?[]
        private object? _actions;
        private int _count;
        private FastSpinLock _lock;

        public int SubscibedCount => _count;

        internal EventHandlerHolder()
        {
        }

        public void Invoke(T arg)
        {
            // When _count == 0, there is no need to perform exclusive locking.
            if(_count == 0) { return; }

            _lock.Enter();          // ---- enter
            // Get _count after exclusive locking.
            var count = _count;
            if(count == 1) {
                var action = SafeCast.NotNullAs<Action<T>>(_actions);
                _lock.Exit();       // ---- exit
                action.Invoke(arg);
                return;
            }
            else {
                using var mem = new RefTypeRentMemory<object?>(count);
                Span<object?> memSpan;
                try {
                    var actions = SafeCast.NotNullAs<object?[]>(_actions).AsSpan(0, count);
                    memSpan = mem.AsSpan();
                    actions.CopyTo(memSpan);
                }
                finally {
                    _lock.Exit();   // ---- exit
                }
                foreach(var action in memSpan) {
                    SafeCast.NotNullAs<Action<T>>(action).Invoke(arg);
                }
                return;
            }
        }

        public void Clear()
        {
            _lock.Enter();          // ---- enter
            _count = 0;
            _actions = null;
            _lock.Exit();           // ---- exit
        }

        public Action<T> ToDelegate()
        {
            return Invoke;
        }

        internal void Subscribe(Action<T> action)
        {
            Debug.Assert(action is not null);
            _lock.Enter();          // ---- enter
            try {
                var count = _count;
                if(count == 0) {
                    Debug.Assert(_actions is null);
                    _actions = action;
                }
                else if(count == 1) {
                    Debug.Assert(_actions is Action<T>);

                    if(ArrayPoolForEventSource.TryGetArray4Fast(out var actions) == false) {
                        actions = new object[DefaultBufferCapacity];
                    }
                    Debug.Assert(actions.Length >= 2);
                    MemoryMarshal.GetArrayDataReference(actions) = _actions;
                    Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(actions), 1) = action;
                    _actions = actions;
                }
                else {
                    var actions = SafeCast.NotNullAs<object?[]>(_actions);
                    if(actions.Length == count) {
                        var newActions = new object[actions.Length * 2];
                        actions.AsSpan().CopyTo(newActions);
                        if(actions.Length == ArrayPoolForEventSource.LengthOfPoolTargetArray) {
                            actions[0] = null;
                            actions[1] = null;
                            actions[2] = null;
                            actions[3] = null;
                            ArrayPoolForEventSource.ReturnArray4Fast(actions);
                        }
                        _actions = newActions;
                        actions = newActions;
                    }
                    actions[count] = action;
                }
                _count = count + 1;
            }
            finally {
                _lock.Exit();       // ---- exit
            }
        }

        internal void Unsubscribe(Action<T>? action)
        {
            if(action is null) { return; }
            _lock.Enter();          // ---- enter
            try {
                var actions = _actions;
                var count = _count;
                if(count == 0) {
                    Debug.Assert(actions is null);
                    return;
                }
                else if(count == 1) {
                    if(ReferenceEquals(_actions, action)) {
                        _count = 0;
                        _actions = null;
                    }
                    return;
                }
                else {
                    var actionSpan = SafeCast.NotNullAs<object?[]>(actions).AsSpan(0, count);
                    for(int i = 0; i < actionSpan.Length; i++) {
                        if(ReferenceEquals(actionSpan[i], action)) {
                            _count = count - 1;
                            if(i < _count) {
                                var copyLen = _count - i;
                                actionSpan.Slice(i + 1, copyLen).CopyTo(actionSpan.Slice(i));
                            }
                            actionSpan[_count] = null;
                            if(_count == 1) {
                                var a = actionSpan[0];
                                var actionsArray = SafeCast.NotNullAs<object?[]>(actions);
                                if(actionsArray.Length == ArrayPoolForEventSource.LengthOfPoolTargetArray) {
                                    actionsArray[0] = null;
                                    actionsArray[1] = null;
                                    actionsArray[2] = null;
                                    actionsArray[3] = null;
                                    ArrayPoolForEventSource.ReturnArray4Fast(actionsArray);
                                }
                                _actions = a;
                            }
                            return;
                        }
                    }
                    return;
                }
            }
            finally {
                _lock.Exit();       // ---- exit
            }
        }
    }
}
