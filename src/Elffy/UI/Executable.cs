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

        internal void Execute(ExecutableExecuteType type)
        {
            switch(type) {
                case ExecutableExecuteType.KeyDown:
                    KeyDown?.Invoke(this);
                    break;
                case ExecutableExecuteType.KeyPress:
                    KeyPress?.Invoke(this);
                    break;
                case ExecutableExecuteType.KeyUp:
                    KeyUp?.Invoke(this);
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary><see cref="Executable"/> の実行のタイプ</summary>
    internal enum ExecutableExecuteType
    {
        KeyDown,
        KeyPress,
        KeyUp
    }
}
