#nullable enable
using OpenTK.Windowing.Common;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;
using Elffy.Effective.Unsafes;
using TKKeys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace Elffy.InputSystem
{
    public sealed class Keyboard
    {
        private const int KeyLen = (int)TKKeys.LastKey + 1;
        private BitArrayUnsafe _p;      // prev frame
        private BitArrayUnsafe _c;      // current frame
        private BitArrayUnsafe _n;      // next frame
        private BitArrayUnsafe _nSub;   // next frame (but not copied from current frame buffer on move to next frame.)
        private KeyModifiers _cm;
        private KeyModifiers _nm;

        private readonly Queue<KeyboardKeyEventArgs> _retry;

        internal Keyboard()
        {
            _p = new BitArrayUnsafe(KeyLen);
            _c = new BitArrayUnsafe(KeyLen);
            _n = new BitArrayUnsafe(KeyLen);
            _nSub = new BitArrayUnsafe(KeyLen);
            _retry = new Queue<KeyboardKeyEventArgs>(KeyLen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDown(Keys key)
        {
            if((uint)key >= KeyLen) { return false; }
            int index = (int)key;
            return _c[index] && !_p[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPress(Keys key)
        {
            if((uint)key >= KeyLen) { return false; }
            int index = (int)key;
            return _c[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPress(Keys key, KeyModifiers mod)
        {
            if((uint)key >= KeyLen) { return false; }
            int index = (int)key;

            // No Boxing happen by optimization in 'HasFlag'.
            return _c[index] && _cm.HasFlag(mod);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUp(Keys key)
        {
            if((uint)key >= KeyLen) { return false; }
            int index = (int)key;
            return !_c[index] && _p[index];
        }

        internal void ChangeToDown(in KeyboardKeyEventArgs e)
        {
            if((uint)e.Key >= KeyLen) { return; }
            if(e.IsRepeat) { return; }

            int index = (int)e.Key;
            _n[index] = true;
            _nSub[index] = true;
            _nm = (KeyModifiers)e.Modifiers;
        }

        internal void ChangeToUp(in KeyboardKeyEventArgs e)
        {
            if((uint)e.Key >= (uint)KeyLen) { return; }

            int index = (int)e.Key;
            if(_nSub[index]) {
                // If the key got pressed at the same frame, put event in a queue and retry at next frame.
                _retry.Enqueue(e);
            }
            else {
                _n[index] = false;
                _nm = (KeyModifiers)e.Modifiers;
            }
        }

        internal void InitFrame()
        {
            (_p, _c, _n) = (_c, _n, _p);
            _c.CopyTo(_n);      // Next frame buffer takes over the states in current buffer.
            _nSub.Clear();      // Sub buffer is cleared. (not take over them.)

            _cm = _nm;

            // Retry event in the queue.
            foreach(var e in _retry) {
                ChangeToUp(e);
            }
            _retry.Clear();
        }


        // This array does not check boudary. This is unsafe. Be careful
        [DebuggerTypeProxy(typeof(BitArrayDebuggerTypeProxy))]
        [DebuggerDisplay("BitArray[{Length}]")]
        internal readonly struct BitArrayUnsafe
        {
            private readonly int _length;
            private readonly byte[] _array;

            public int Length => _length;

            public bool this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    var a = Math.DivRem(index, 8, out var mod);
                    var mask = (byte)(0x01 << mod);
                    return (_array.At(a) & mask) == mask;
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    var a = Math.DivRem(index, 8, out var mod);
                    var mask = (0x01 << mod);
                    if(value) {
                        _array.At(a) |= (byte)mask;
                    }
                    else {
                        _array.At(a) &= (byte)~(mask);
                    }
                }
            }

            public BitArrayUnsafe(int length)
            {
                _length = length;
                _array = new byte[length / 8 + 1];
            }

            public void CopyTo(in BitArrayUnsafe dest)
            {
                _array.CopyTo(dest._array, 0);
            }

            public void Clear()
            {
                Array.Clear(_array, 0, _array.Length);
            }

            private bool[] ToBoolArray()
            {
                // this method is only for debug.
                var boolArray = new bool[_length];
                for(int i = 0; i < _length; i++) {
                    boolArray[i] = this[i];
                }
                return boolArray;
            }

            internal class BitArrayDebuggerTypeProxy
            {
                [DebuggerBrowsable(DebuggerBrowsableState.Never)]
                private readonly BitArrayUnsafe _entity;

                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public bool[] Values => _entity.ToBoolArray();

                public BitArrayDebuggerTypeProxy(BitArrayUnsafe entity) => _entity = entity;
            }
        }
    }
}
