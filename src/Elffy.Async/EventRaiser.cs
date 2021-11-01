#nullable enable
using Elffy.Effective;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public sealed class EventRaiser<T>
    {
        private const int DefaultBufferCapacity = 4;

        // [NOTE]
        // _count == 0      || 1         || n (n >= 2)
        // _actions == null || Action<T> || Action<T>[]
        private object? _actions;
        private int _count;
        private FastSpinLock _lock;

        public int SubscibedCount => _count;

        public EventRaiser()
        {
        }

        public void Raise(T arg)
        {
            _lock.Enter();          // ---- enter
            var count = _count;
            if(count == 0) {
                _lock.Exit();       // ---- exit
                return;
            }
            else if(count == 1) {
                var action = SafeCast.NotNullAs<Action<T>>(_actions);
                _lock.Exit();       // ---- exit
                action.Invoke(arg);
                return;
            }
            else {
                var mem = new RefTypeRentMemory<Action<T>>(count);
                try {
                    var actions = SafeCast.NotNullAs<Action<T>[]>(_actions).AsSpan(0, count);
                    actions.CopyTo(mem.AsSpan());
                }
                finally {
                    mem.Dispose();
                    _lock.Exit();   // ---- exit
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
            return Raise;
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
                    var actions = new Action<T>[DefaultBufferCapacity];
                    actions[0] = Unsafe.As<Action<T>>(_actions);
                    actions[1] = action;
                    _actions = actions;
                }
                else {
                    var actions = SafeCast.NotNullAs<Action<T>[]>(_actions);
                    if(actions.Length == count) {
                        var newActions = new Action<T>[actions.Length * 2];
                        actions.AsSpan().CopyTo(newActions);
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
                    var actionSpan = SafeCast.NotNullAs<Action<T>[]>(actions).AsSpan(0, count);
                    for(int i = 0; i < actionSpan.Length; i++) {
                        if(actionSpan[i] == action) {
                            _count = count - 1;
                            if(i < _count) {
                                var copyLen = _count - i;
                                actionSpan.Slice(i + 1, copyLen).CopyTo(actionSpan.Slice(i));
                            }
                            actionSpan[_count] = null!;
                            if(_count == 1) {
                                _actions = actionSpan[0];
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
