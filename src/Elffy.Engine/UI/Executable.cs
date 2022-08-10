#nullable enable
using System.Runtime.CompilerServices;
using Elffy.InputSystem;

namespace Elffy.UI
{
    /// <summary>Base class which fires event on mouse click.</summary>
    public abstract class Executable : Control
    {
        private bool _keyPressed;
        private bool _isEventFlowCanceled;
        private EventSource<Executable>? _keyDown;
        private EventSource<Executable>? _keyPress;
        private EventSource<Executable>? _keyUp;

        public bool IsKeyPressed => _keyPressed;

        public Event<Executable> KeyDown => new Event<Executable>(ref _keyDown);
        public Event<Executable> KeyPress => new Event<Executable>(ref _keyPress);
        public Event<Executable> KeyUp => new Event<Executable>(ref _keyUp);

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
                    InvokeEvent(_keyUp, this);
                }
                return;
            }

            var isMouseOver = IsMouseOver;
            if((isMouseOver || _keyPressed) == false) { return; }

            if(TryGetScreen(out var screen) == false) { return; }
            var mouse = screen.Mouse;
            if(mouse.IsPressed(MouseButton.Left)) {
                if(isMouseOver) {
                    if(_keyPressed) {
                        InvokeEvent(_keyPress, this);
                    }
                    else {
                        if(mouse.IsDown(MouseButton.Left)) {
                            _keyPressed = true;
                            InvokeEvent(_keyDown, this);
                            InvokeEvent(_keyPress, this);
                        }
                    }
                }
                else if(_keyPressed) {
                    InvokeEvent(_keyPress, this);
                }
            }
            else {
                if(_keyPressed) {
                    _keyPressed = false;
                    InvokeEvent(_keyUp, this);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InvokeEvent(EventSource<Executable>? source, Executable arg)
        {
            try {
                source?.Invoke(arg);
            }
            catch {
                if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
                // Don't throw, ignore exceptions in user code.
            }
        }
    }
}
