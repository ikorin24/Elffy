#nullable enable
using Elffy.Effective;
using Elffy.Exceptions;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using OpenTKKey = OpenTK.Input.Key;

namespace Elffy.Input
{
    public static class Input
    {
        #region private member
        /// <summary>Axisのデフォルトの最小値</summary>
        private const float DEFAULT_AXIS_MIN_VALUE = 0.1f;
        /// <summary>ゲームパッドの状態</summary>
        private static PadState _pad = new PadState();
        /// <summary>キーボードの状態</summary>
        private static KeyboardState _keyboard;
        /// <summary>入力状態名と関連情報</summary>
        private static Dictionary<string, StateNameObject> _stateNames = new Dictionary<string, StateNameObject>();
        /// <summary>入力軸状態名と関連情報</summary>
        private static Dictionary<string, AxisNameObject> _axisNames = new Dictionary<string, AxisNameObject>();
        /// <summary>入力トリガー状態名と関連情報</summary>
        private static Dictionary<string, TriggerNameObject> _triggerNames = new Dictionary<string, TriggerNameObject>();
        /// <summary>各入力状態の現在の状態</summary>
        private static Dictionary<string, bool> _currentState = new Dictionary<string, bool>();
        /// <summary>各入力状態の前回の状態</summary>
        private static Dictionary<string, bool> _previousState = new Dictionary<string, bool>();
        /// <summary各入力軸状態の現在の状態</summary>
        private static Dictionary<string, float> _currentAxis = new Dictionary<string, float>();
        /// <summary>各入力軸状態の前回の状態</summary>
        private static Dictionary<string, float> _previousAxis = new Dictionary<string, float>();
        /// <summary>各入力トリガーの現在の状態</summary>
        private static Dictionary<string, float> _currentTrigger = new Dictionary<string, float>();
        /// <summary>各入力トリガーの前回の状態</summary>
        private static Dictionary<string, float> _previousTrigger = new Dictionary<string, float>();
        #endregion private member

        #region public Method
        #region GetState
        /// <summary>現在の入力状態を取得します</summary>
        /// <param name="name">状態名</param>
        /// <returns>入力状態</returns>
        public static bool GetState(string name)
        {
            var result = _currentState.GetValueWithKeyChecking(name, "Specified input state is not registered.");
            return result;
        }
        #endregion

        #region GetStateDown
        /// <summary>現在のフレームで状態が入力されたかどうかを取得します（ボタン入力されたフレームのみtrue）</summary>
        /// <param name="name">状態名</param>
        /// <returns>入力状態</returns>
        public static bool GetStateDown(string name)
        {
            var current = _currentState.GetValueWithKeyChecking(name, "Specified input state is not registered.");
            var prev = _previousState.GetValueWithKeyChecking(name, "Specified input state is not registered.");
            return current && !prev;        // 立ち上がりを検出
        }
        #endregion

        #region GetStateUp
        /// <summary>現在のフレームで状態が入力解除されたかどうかを取得します（ボタン入力解除されたフレームのみtrue）</summary>
        /// <param name="name">状態名</param>
        /// <returns>結果</returns>
        public static bool GetStateUp(string name)
        {
            var current = _currentState.GetValueWithKeyChecking(name, "Specified input state is not registered.");
            var prev = _previousState.GetValueWithKeyChecking(name, "Specified input state is not registered.");
            return !current && prev;        // 立ち下がりを検出
        }
        #endregion

        #region GetAxis
        /// <summary>現在の入力軸状態を取得します</summary>
        /// <param name="name">状態名</param>
        /// <returns>入力値</returns>
        public static float GetAxis(string name)
        {
            var value = _currentAxis.GetValueWithKeyChecking(name, "Specified input state is not registered.");
            return value;
        }
        #endregion

        public static float GetTrigger(string name)
        {
            var value = _currentTrigger.GetValueWithKeyChecking(name, "Specified input state is not registered.");
            return value;
        }

        #region AddState
        /// <summary>入力状態を登録します</summary>
        /// <param name="name">状態名</param>
        /// <param name="key">関連させるキーボードのキー</param>
        /// <param name="gamepadButton">関連させるゲームパッドのボタン番号</param>
        public static void AddState(string name, Key key, int gamepadButton)
        {
            ArgumentChecker.ThrowIfNullArg(name, nameof(name));
            ArgumentChecker.ThrowOutOfRangeIf(gamepadButton < 0 || gamepadButton >= PadState.BUTTON_COUNT, nameof(gamepadButton), gamepadButton, $"{nameof(gamepadButton)} is out of range.");
            ArgumentChecker.ThrowArgumentIf(_stateNames.ContainsKey(name), $"Input state '{name}' already exists".AsInterned());
            _stateNames.Add(name, new StateNameObject(name, key, gamepadButton));
            _currentState.Add(name, false);
            _previousState.Add(name, false);
        }
        #endregion

        #region AddAxis
        /// <summary>入力軸状態を登録します</summary>
        /// <param name="name">状態名</param>
        /// <param name="positiveKey">軸の正方向に関連させるキーボードのキー</param>
        /// <param name="negativeKey">軸の負方向に関連させるキーボードのキー</param>
        /// <param name="stickAxis">関連させるゲームパッドの軸</param>
        public static void AddAxis(string name, Key positiveKey, Key negativeKey, StickAxis stickAxis)
            => AddAxis(name, positiveKey, negativeKey, stickAxis, DEFAULT_AXIS_MIN_VALUE);

        /// <summary>入力軸状態を登録します</summary>
        /// <param name="name">状態名</param>
        /// <param name="positiveKey">軸の正方向に関連させるキーボードのキー</param>
        /// <param name="negativeKey">軸の負方向に関連させるキーボードのキー</param>
        /// <param name="stickAxis">関連させるゲームパッドの軸</param>
        /// <param name="minValue">軸が反応する最小値</param>
        public static void AddAxis(string name, Key positiveKey, Key negativeKey, StickAxis stickAxis, float minValue)
        {
            ArgumentChecker.ThrowIfNullArg(name, nameof(name));
            ArgumentChecker.ThrowOutOfRangeIf(minValue < 0 || minValue > 1, nameof(minValue), minValue, $"{nameof(minValue)} must be between 0 and 1");
            ArgumentChecker.ThrowArgumentIf(_axisNames.ContainsKey(name), $"Input state '{name}' already exists".AsInterned());
            _axisNames.Add(name, new AxisNameObject(name, positiveKey, negativeKey, stickAxis, minValue));
            _currentAxis.Add(name, 0);
            _previousAxis.Add(name, 0);
        }
        #endregion

        #region AddTrigger
        public static void AddTrigger(string name, Key key, Trigger trigger) => AddTrigger(name, key, trigger, DEFAULT_AXIS_MIN_VALUE);

        public static void AddTrigger(string name, Key key, Trigger trigger, float minValue)
        {
            ArgumentChecker.ThrowIfNullArg(name, nameof(name));
            ArgumentChecker.ThrowOutOfRangeIf(minValue < 0 || minValue > 1, nameof(minValue), minValue, $"{nameof(minValue)} must be between 0 and 1");
            ArgumentChecker.ThrowArgumentIf(_triggerNames.ContainsKey(name), $"Input state '{name}' already exists".AsInterned());
            _triggerNames.Add(name, new TriggerNameObject(name, key, trigger, minValue));
            _currentTrigger.Add(name, 0);
            _previousTrigger.Add(name, 0);
        }
        #endregion
        #endregion

        #region internal Method
        #region Update
        /// <summary>入力の状態を更新します</summary>
        internal static void Update()
        {
            _pad.Parse(GamePad.GetState(0));
            _keyboard = Keyboard.GetState();

            // Stateを更新
            foreach(var sn in _stateNames) {
                _previousState[sn.Key] = _currentState[sn.Key];
                _currentState[sn.Key] = _pad.Button[sn.Value.GamepadButton] | _keyboard[(OpenTKKey)sn.Value.Key];
            }
            // Axisを更新
            foreach(var an in _axisNames) {
                float padValue = 0f;
                switch(an.Value.Axis) {
                    case StickAxis.LeftStickX:
                        padValue = _pad.LeftStick.X;
                        break;
                    case StickAxis.LeftStickY:
                        padValue = _pad.LeftStick.Y;
                        break;
                    case StickAxis.RightStickX:
                        padValue = _pad.RightStick.X;
                        break;
                    case StickAxis.RightStickY:
                        padValue = _pad.RightStick.Y;
                        break;
                    default:
                        break;
                }
                if(Math.Abs(padValue) < an.Value.MinValue) {
                    padValue = 0f;
                }
                float keyboardValue = (_keyboard[(OpenTKKey)an.Value.PositiveKey] ? 1 : 0) - (_keyboard[(OpenTKKey)an.Value.NegativeKey] ? 1 : 0);
                _previousAxis[an.Key] = _currentAxis[an.Key];
                _currentAxis[an.Key] = Math.Abs(padValue) < Math.Abs(keyboardValue) ? keyboardValue : padValue;
            }
            // Triggerを更新
            foreach(var tn in _triggerNames) {
                float value = 0f;
                var name = tn.Key;
                switch(tn.Value.Trigger) {
                    case Trigger.RightTrigger:
                        value = _pad.RightTrigger;
                        break;
                    case Trigger.LeftTrigger:
                        value = _pad.LeftTrigger;
                        break;
                    default:
                        break;
                }
                if(Math.Abs(value) < tn.Value.MinValue) {
                    value = 0f;
                }
                float keyboardValue = _keyboard[(OpenTKKey)tn.Value.Key] ? 1 : 0;
                _previousTrigger[tn.Key] = _currentTrigger[tn.Key];
                _currentTrigger[tn.Key] = value < keyboardValue ? keyboardValue : value;
            }
        }
        #endregion

        ///// <summary>デバッグ用</summary>
        //[Conditional("DEBUG")]
        //public static void PadDump()
        //{
        //    _pad.Parse(GamePad.GetState(0));
        //    foreach(var b in _pad.Button) {
        //        DebugManager.Append(b ? 1 : 0);
        //    }
        //    DebugManager.Append($"({_pad.LeftStick.X:N2},{_pad.LeftStick.Y:N2})");
        //    DebugManager.Append($"({_pad.RightStick.X:N2},{_pad.RightStick.Y:N2})");
        //    DebugManager.Append($"({_pad.LeftTrigger:N2}, {_pad.RightTrigger:N2})");
        //}
        #endregion

        #region class StateNameObject
        class StateNameObject
        {
            public string Name { get; private set; }
            public Key Key { get; private set; }
            public int GamepadButton { get; private set; }

            public StateNameObject(string name, Key key, int gamepadButton)
            {
                Name = name;
                Key = key;
                GamepadButton = gamepadButton;
            }
        }
        #endregion

        #region class AxisNameObject
        class AxisNameObject
        {
            public string Name { get; private set; }
            public Key PositiveKey { get; private set; }
            public Key NegativeKey { get; private set; }
            public StickAxis Axis { get; private set; }
            public float MinValue { get; private set; }

            public AxisNameObject(string name, Key positiveKey, Key negativeKey, StickAxis axis, float minValue)
            {
                Name = name;
                PositiveKey = positiveKey;
                NegativeKey = negativeKey;
                Axis = axis;
                MinValue = minValue;
            }
        }
        #endregion

        #region class TriggerNameObject
        class TriggerNameObject
        {
            public string Name { get; private set; }
            public Key Key { get; private set; }
            public Trigger Trigger { get; private set; }
            public float MinValue { get; private set; }

            public TriggerNameObject(string name, Key key, Trigger trigger, float minValue)
            {
                Name = name;
                Key = key;
                Trigger = trigger;
                MinValue = minValue;
            }
        }
        #endregion
    }
}
