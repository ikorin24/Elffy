#nullable enable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenTK;
using OpenTK.Input;

namespace Elffy.InputSystem
{
    #region class PadState
    class PadState
    {
        public const int BUTTON_COUNT = 15;

        private bool[] _button = new bool[BUTTON_COUNT];
        public IReadOnlyList<bool> Button { get; private set; }
        public bool IsAnyButtonPressed { get; private set; }
        public Vector2 LeftStick { get; private set; }
        public Vector2 RightStick { get; private set; }
        public float LeftTrigger { get; private set; }
        public float RightTrigger { get; private set; }

        public PadState()
        {
            Button = new ReadOnlyCollection<bool>(_button);
        }

        #region Parse
        /// <summaryGapPadStateからの読み取り</summary>
        /// <param name="state">GamePadState</param>
        public void Parse(GamePadState state)
        {
            IsAnyButtonPressed = state.Buttons.IsAnyButtonPressed;

            // 各ボタン
            _button[0] = state.Buttons.A == ButtonState.Pressed;
            _button[1] = state.Buttons.B == ButtonState.Pressed;
            _button[2] = state.Buttons.X == ButtonState.Pressed;
            _button[3] = state.Buttons.Y == ButtonState.Pressed;
            _button[4] = state.Buttons.Start == ButtonState.Pressed;
            _button[5] = state.Buttons.Back == ButtonState.Pressed;
            _button[6] = state.Buttons.BigButton == ButtonState.Pressed;
            _button[7] = state.Buttons.LeftStick == ButtonState.Pressed;
            _button[8] = state.Buttons.LeftShoulder == ButtonState.Pressed;
            _button[9] = state.Buttons.RightStick == ButtonState.Pressed;
            _button[10] = state.Buttons.RightShoulder == ButtonState.Pressed;

            // 十字キー
            _button[11] = state.DPad.IsUp;
            _button[12] = state.DPad.IsDown;
            _button[13] = state.DPad.IsRight;
            _button[14] = state.DPad.IsLeft;

            // スティック
            LeftStick = state.ThumbSticks.Left;
            RightStick = state.ThumbSticks.Right;

            // トリガー
            LeftTrigger = state.Triggers.Left;
            RightTrigger = state.Triggers.Right;
        }
        #endregion
    }
    #endregion
}
