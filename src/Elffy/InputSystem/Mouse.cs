#nullable enable
using System.Runtime.CompilerServices;

namespace Elffy.InputSystem
{
    /// <summary>Provides the mouse state and functions</summary>
    public sealed class Mouse
    {
        private KeyBuffer _leftButton;
        private KeyBuffer _rightButton;
        private KeyBuffer _middleButton;
        private WheelBuffer _wheel;
        private PositionBuffer _positionBuffer;
        private bool _onScreen;

        /// <summary>Get whether the mouse is on the screen or not.</summary>
        public bool OnScreen => _onScreen;

        /// <summary>Get position of the mouse on the screen based on top-left.</summary>
        public Vector2 Position => _positionBuffer.Current;

        public Vector2 PositionDelta => _positionBuffer.Delta;

        /// <summary>Get wheel value difference from previouse frame.</summary>
        public float WheelDelta => _wheel.Wheel;

        internal Mouse() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ChangePosition(in Vector2 position)
        {
            _positionBuffer.SetValue(position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ChangePressedState(MouseButton button, bool isPressed)
        {
            // The following code is faster than 'switch' when the method is inlined.
            if(button == MouseButton.Left) {
                _leftButton.SetValue(isPressed);
            }
            else if(button == MouseButton.Right) {
                _rightButton.SetValue(isPressed);
            }
            else if(button == MouseButton.Middle) {
                _middleButton.SetValue(isPressed);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ChangeWheel(float value) => _wheel.SetValue(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ChangeOnScreen(bool onScreen) => _onScreen = onScreen;

        internal void InitFrame()
        {
            _positionBuffer.InitFrame();
            _leftButton.InitFrame();
            _rightButton.InitFrame();
            _middleButton.InitFrame();
            _wheel.InitFrame();
        }

        /// <summary>Get whether specified mouse button is down. (Return true if the button got pressed on this frame.)</summary>
        /// <param name="button">mouse button</param>
        /// <returns>button down state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDown(MouseButton button)
        {
            // The following code is faster than 'switch' when the method is inlined.
            if(button == MouseButton.Left) {
                return _leftButton.IsKeyDown();
            }
            else if(button == MouseButton.Right) {
                return _rightButton.IsKeyDown();
            }
            else if(button == MouseButton.Middle) {
                return _middleButton.IsKeyDown();
            }
            else {
                return false;
            }
        }

        /// <summary>Get whether specified mouse button is pressed.</summary>
        /// <param name="button">mouse button</param>
        /// <returns>button pressed state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPressed(MouseButton button)
        {
            // The following code is faster than 'switch' when the method is inlined.
            if(button == MouseButton.Left) {
                return _leftButton.IsKeyPressed();
            }
            else if(button == MouseButton.Right) {
                return _rightButton.IsKeyPressed();
            }
            else if(button == MouseButton.Middle) {
                return _middleButton.IsKeyPressed();
            }
            else {
                return false;
            }
        }

        /// <summary>Get whether specified mouse button Tis up. (Return true if the button got released on this frame.)</summary>
        /// <param name="button">mouse button</param>
        /// <returns>button up state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUp(MouseButton button)
        {
            // The following code is faster than 'switch' when the method is inlined.
            if(button == MouseButton.Left) {
                return _leftButton.IsKeyUp();
            }
            else if(button == MouseButton.Right) {
                return _rightButton.IsKeyUp();
            }
            else if(button == MouseButton.Middle) {
                return _middleButton.IsKeyUp();
            }
            else {
                return false;
            }
        }

        private struct KeyBuffer
        {
            private bool _current;
            private bool _prev;
            private bool _changed;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetValue(bool value)
            {
                _prev = _current;
                _current = value;
                _changed = true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsKeyPressed() => _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsKeyDown() => _current && !_prev;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsKeyUp() => !_current && _prev;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void InitFrame()
            {
                if(_changed == false) {
                    _prev = _current;
                }
                _changed = false;
            }
        }

        private struct WheelBuffer
        {
            private float _current;
            private bool _changed;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetValue(float value)
            {
                _current = value;
                _changed = true;
            }

            public float Wheel => _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void InitFrame()
            {
                if(_changed == false) {
                    _current = 0f;
                }
                _changed = false;
            }
        }

        private struct PositionBuffer
        {
            private Vector2 _delta;
            private Vector2 _current;
            private Vector2 _newValue;
            private bool _changed;

            public Vector2 Current => _current;

            public Vector2 Delta => _delta;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetValue(Vector2 value)
            {
                _newValue = value;
                _changed = true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void InitFrame()
            {
                if(_changed) {
                    _delta = _newValue - _current;
                    _current = _newValue;
                }
                else {
                    _delta = default;
                }
                _changed = false;
            }
        }
    }
}
