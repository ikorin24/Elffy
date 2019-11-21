#nullable enable

namespace Elffy.UI
{
    /// <summary>実行可能なUIの基底クラス</summary>
    public abstract class Executable : Control
    {
        /// <summary>Key down event</summary>
        public event ActionEventHandler<Executable>? KeyDown;
        /// <summary>Key press event</summary>
        public event ActionEventHandler<Executable>? KeyPress;
        /// <summary>Key up event</summary>
        public event ActionEventHandler<Executable>? KeyUp;

        public Executable()
        {
            Renderable.IsFrozen = false;
            Renderable.Updated += OnUpdated;
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

        private void OnUpdated(FrameObject sender)
        {
        }
    }
}
