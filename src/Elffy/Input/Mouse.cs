using System;
using System.Drawing;
using Elffy.Core;
using OpenTK.Input;
using TKMouseButton = OpenTK.Input.MouseButton;

namespace Elffy.Input
{
    /// <summary>マウスの状態を表すクラスです</summary>
    public class Mouse
    {
        private MouseState _state;
        /// <summary>マウスが <see cref="IScreenHost"/> の描画領域内にあるかどうかを取得します</summary>
        public bool OnScreen { get; private set; }

        /// <summary>Top-Left を基点とする、マウスの <see cref="IScreenHost"/> 内での座標を取得します</summary>
        public Point Position => new Point(_state.X, _state.Y);

        internal Mouse()
        {
        }

        /// <summary>現在の状態を更新します</summary>
        /// <param name="state">マウスの状態</param>
        internal void Update(MouseState state)
        {
            _state = state;
        }

        /// <summary>現在のマウスが <see cref="IScreenHost"/> の描画領域内にあるかどうかを更新します</summary>
        /// <param name="onScreen">マウスが領域内にあるかどうか</param>
        internal void Update(bool onScreen)
        {
            OnScreen = onScreen;
        }

        public bool IsPressed(MouseButton button)
        {
            switch(button) {
                case MouseButton.Left:
                    return _state.IsButtonDown(TKMouseButton.Left);
                case MouseButton.Right:
                    return _state.IsButtonDown(TKMouseButton.Right);
                default:
                    throw new NotSupportedException($"Unknown type '{button}'");
            }
        }
    }

    public enum MouseButton
    {
        Left,
        Right,
    }
}
