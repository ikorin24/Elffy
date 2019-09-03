using System;

namespace Elffy.UI
{
    #region class Executable
    /// <summary>実行可能なUIの基底クラス</summary>
    public abstract class Executable : UIBase
    {
        /// <summary>Key down event</summary>
        public event EventHandler KeyDown;
        /// <summary>Key press event</summary>
        public event EventHandler KeyPress;
        /// <summary>Key up event</summary>
        public event EventHandler KeyUp;

        internal void Execute(ExecutableExecuteType type)
        {
            switch(type) {
                case ExecutableExecuteType.KeyDown:
                    KeyDown?.Invoke(this, EventArgs.Empty);
                    break;
                case ExecutableExecuteType.KeyPress:
                    KeyPress?.Invoke(this, EventArgs.Empty);
                    break;
                case ExecutableExecuteType.KeyUp:
                    KeyUp?.Invoke(this, EventArgs.Empty);
                    break;
                default:
                    break;
            }
        }
    }
    #endregion class Executable

    #region enum ExecutableExecuteType
    /// <summary><see cref="Executable"/> の実行のタイプ</summary>
    internal enum ExecutableExecuteType
    {
        KeyDown,
        KeyPress,
        KeyUp
    }
    #endregion
}
