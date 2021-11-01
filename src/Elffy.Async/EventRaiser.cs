#nullable enable
using Elffy.Effective;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    public sealed class EventRaiser<T>
    {
        private const int DefaultBufferCapacity = ArrayPoolForEventRaiser.LengthOfPoolTargetArray;

        // [NOTE]
        // _count == 0      || 1         || n (n >= 2)
        // _actions == null || Action<T> || object?[]
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

                    if(ArrayPoolForEventRaiser.TryGetInstanceFast(out var actions) == false) {
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
                        if(actions.Length == ArrayPoolForEventRaiser.LengthOfPoolTargetArray) {
                            actions[0] = null;
                            actions[1] = null;
                            actions[2] = null;
                            actions[3] = null;
                            ArrayPoolForEventRaiser.ReturnInstanceFast(actions);
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
                                if(actionsArray.Length == ArrayPoolForEventRaiser.LengthOfPoolTargetArray) {
                                    actionsArray[0] = null;
                                    actionsArray[1] = null;
                                    actionsArray[2] = null;
                                    actionsArray[3] = null;
                                    ArrayPoolForEventRaiser.ReturnInstanceFast(actionsArray);
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

    internal static class ArrayPoolForEventRaiser
    {
        internal const int LengthOfPoolTargetArray = 4; // Don't change

        private const int MaxPoolingCount = 512;
        private static FastSpinLock _lock;
        private static object?[]? _root;
        private static int _pooledCount;

        public static bool TryGetInstanceFast([MaybeNullWhen(false)] out object?[] instance)
        {
            // If the exclusion control is successfully obtained, I try to get the instance from the pool.
            if(_lock.TryEnter() == false) {
                instance = null;
                return false;
            }
            try {
                instance = _root;
                if(instance is not null) {
                    Debug.Assert(_pooledCount > 0);
                    Debug.Assert(instance.Length == LengthOfPoolTargetArray);
                    _pooledCount--;
                    _root = SafeCast.As<object?[]>(MemoryMarshal.GetArrayDataReference(instance));  // _root = (object?[])instance[0];
                    MemoryMarshal.GetArrayDataReference(instance) = null;                           // instance[0] = null;
                    return true;
                }
                return false;
            }
            finally {
                _lock.Exit();
            }
        }

        public static void ReturnInstanceFast(object?[] instance)
        {
            Debug.Assert(instance.Length == LengthOfPoolTargetArray);

            // If the exclusion control is successfully obtained, add the instance to the pool.
            if(_lock.TryEnter() == false) {
                return;
            }
            try {
                var pooledCount = _pooledCount;
                if(pooledCount == MaxPoolingCount) { return; }
                var root = _root;
                _pooledCount = pooledCount + 1;
                _root = instance;
                if(root is not null) {
                    MemoryMarshal.GetArrayDataReference(instance) = root;   // instance[0] = root;
                }
            }
            finally {
                _lock.Exit();
            }
        }
    }
}
