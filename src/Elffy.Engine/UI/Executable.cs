#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.InputSystem;

namespace Elffy.UI
{
    /// <summary>Base class which fires event on mouse click.</summary>
    public abstract class Executable : Control
    {
        private bool _keyPressed;
        private bool _isEventFlowCanceled;

        public bool IsKeyPressed => _keyPressed;

        /// <summary>Key down event</summary>
        public event Action<Executable>? KeyDown;
        /// <summary>Key press event</summary>
        public event Action<Executable>? KeyPress;
        /// <summary>Key up event</summary>
        public event Action<Executable>? KeyUp;

        public Executable()
        {
        }

        protected override void OnUIEvent()
        {
            base.OnUIEvent();
            OnExecutableEvent();
        }

        protected private void ForceCancelExecutableEventFlow()
        {
            _isEventFlowCanceled = true;
        }

        private void OnExecutableEvent()
        {
            if(_isEventFlowCanceled) {
                if(_keyPressed) {
                    _keyPressed = false;
                    _isEventFlowCanceled = false;
                    FireEvent(KeyUp, this);
                }
                return;
            }

            var isMouseOver = IsMouseOver;
            // ヒットテストがヒットしていなくても、押しっぱなしなら処理を継続
            if((isMouseOver || _keyPressed) == false) { return; }

            // isMouseOver か _keyPressed の少なくともいずれか1つは true

            if(TryGetScreen(out var screen) == false) { return; }
            var mouse = screen.Mouse;
            if(mouse.IsPressed(MouseButton.Left)) {
                if(isMouseOver) {
                    if(_keyPressed) {
                        FireEvent(KeyPress, this);
                    }
                    else {
                        if(mouse.IsDown(MouseButton.Left)) {
                            _keyPressed = true;
                            FireEvent(KeyDown, this);
                            FireEvent(KeyPress, this);
                        }
                    }
                }
                else if(_keyPressed) {
                    FireEvent(KeyPress, this);
                }
            }
            else {
                if(_keyPressed) {
                    _keyPressed = false;
                    FireEvent(KeyUp, this);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FireEvent(Action<Executable>? action, Executable arg)
        {
            try {
                action?.Invoke(arg);
            }
            catch {
                if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                // Don't throw, ignore exceptions in user code.
            }
        }
    }
}
