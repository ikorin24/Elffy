#nullable enable
using Elffy.Effective;
using System;
using System.Runtime.InteropServices;

namespace Elffy.Framing
{
    public class StateMachine : FrameObject, IDisposable
    {
        private bool _disposed;
        private UnmanagedList<StateMachineState> _states = new UnmanagedList<StateMachineState>();
        private StateMachineState _current;

        public StateMachine()
        {
            Updated += OnUpdated;
        }

        ~StateMachine() => Dispose(false);

        public void AddState(int stateID, Func<int> func) => AddState(new StateMachineState(stateID, func));

        public void AddState(StateMachineState state)
        {
            if(IsActivated) { throw new InvalidOperationException("State must be added before state machine activated."); }
            _states.Add(state);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) { }
                _states.Dispose();
                _disposed = true;
            }
        }

        private void OnUpdated(FrameObject sender)
        {
            var nextStateID = _current.Invoke();
            if(nextStateID == 0) {      // TODO: end stateをちゃんと定義する
                Terminate();
                return;
            }
            else {
                if(_states.TryFind(out var next, state => state.StateID == nextStateID)) {
                    _current = next;
                }
                else {
                    throw new InvalidOperationException($"Invalid state transition. Next state not found. : {_current.StateID} --> {nextStateID}");
                }
            }
        }
    }

    public readonly struct StateMachineState
    {
        public readonly IntPtr FuncPtr;
        public readonly int StateID;

        public StateMachineState(int stateID, Func<int> func)
        {
            StateID = stateID;
            FuncPtr = Marshal.GetFunctionPointerForDelegate((Delegate)func);
        }

        internal int Invoke()
        {
            var func = Marshal.GetDelegateForFunctionPointer<Func<int>>(FuncPtr);
            return func();
        }
    }

    public readonly struct StateID
    {
        private readonly short _id;
        private readonly short _flag;

        public const ushort Start = 0;
        public const ushort End = 1;

        public StateID(short id)
        {
            _id = id;
            _flag = 0;
        }

        //public static implicit operator short(StateID stateId) => stateId._id;
        public static implicit operator StateID(short id) => new StateID(id);
    }
}
