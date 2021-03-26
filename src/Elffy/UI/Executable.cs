#nullable enable
using System;
using Elffy.InputSystem;

namespace Elffy.UI
{
    /// <summary>Base class which fires event on mouse click.</summary>
    public abstract class Executable : Control
    {
        private bool _keyPressed;

        /// <summary>Key down event</summary>
        public event Action<Executable>? KeyDown;
        /// <summary>Key press event</summary>
        public event Action<Executable>? KeyPress;
        /// <summary>Key up event</summary>
        public event Action<Executable>? KeyUp;

        public Executable()
        {
        }

        internal void Execute(ExecutableExecutionType type)
        {
            switch(type) {
                case ExecutableExecutionType.KeyDown:
                    KeyDown?.Invoke(this);
                    break;
                case ExecutableExecutionType.KeyPress:
                    KeyPress?.Invoke(this);
                    break;
                case ExecutableExecutionType.KeyUp:
                    KeyUp?.Invoke(this);
                    break;
                default:
                    break;
            }
        }

        protected override void OnRecieveHitTestResult(bool isHit, Mouse mouse)
        {
            base.OnRecieveHitTestResult(isHit, mouse);

            // ヒットテストがヒットしていなくても、押しっぱなしなら処理を継続
            if((isHit || _keyPressed) == false) { return; }

            // isHit か _keyPressed の少なくともいずれか1つは true

            if(mouse.IsPressed(MouseButton.Left)) {

                if(isHit) {
                    if(_keyPressed) {
                        Execute(ExecutableExecutionType.KeyPress);
                    }
                    else {
                        if(mouse.IsDown(MouseButton.Left)) {
                            _keyPressed = true;
                            Execute(ExecutableExecutionType.KeyDown);
                            Execute(ExecutableExecutionType.KeyPress);
                        }
                    }
                }
                else if(_keyPressed) {
                    Execute(ExecutableExecutionType.KeyPress);
                }
            }
            else {
                if(_keyPressed) {
                    _keyPressed = false;
                    Execute(ExecutableExecutionType.KeyUp);
                }
            }
        }
    }
}
