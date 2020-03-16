#nullable enable
using Elffy.Effective;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Elffy.Framing
{
    public class StateMachine : FrameObject
    {
        private ReadOnlyMemory<StateMachineState> _statesDefinition;    // HACK: 本当はアンマネージに取りたい
        private int _prevStateID;
        private StateMachineState _current;

        public StateMachine(ReadOnlyMemory<StateMachineState> statesDefinition)
        {
            _statesDefinition = statesDefinition;
            Activated += OnActivated;
            Updated += OnUpdated;
        }

        private void OnActivated(FrameObject sender)
        {
            if(_statesDefinition.Length == 0) { throw new InvalidOperationException(); }
            _current = _statesDefinition.Span[0];
            _prevStateID = _current.StateID;
        }

        private void OnUpdated(FrameObject sender)
        {
            var nextStateID = _current.Func(_prevStateID);
            if(nextStateID == StateID.End) {
                Terminate();
                return;
            }
            else {
                // HACK: あらかじめグラフ構築しておき検索が走らないようにすべき
                var next = _statesDefinition.Span.FirstOrNull(state => state.StateID == nextStateID);   // O(N) search
                if(next != null) {
                    _prevStateID = _current.StateID;
                    _current = next.Value;
                }
                else {
                    throw new InvalidOperationException($"Invalid state transition. Next state not found. : {_current.StateID} --> {nextStateID}");
                }
            }
        }
    }

    public readonly struct StateMachineState
    {
        public readonly Func<int, int> Func;
        public readonly int StateID;

        public StateMachineState(int stateID, Func<int, int> func)
        {
            StateID = stateID;
            Func = func;
        }
    }

    public static class StateID
    {
        public static readonly int End = int.MaxValue;
    }
}
