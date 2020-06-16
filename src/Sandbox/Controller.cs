using Elffy.InputSystem;
using Elffy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElffyGame
{
    public static class Controller
    {
        private const string A = "A";
        private const string B = "B";
        private const string X = "X";
        private const string Y = "Y";
        private const string AXIS_X = "AxisX";
        private const string AXIS_Y = "AxisY";
        private const string SUB_AXIS_X = "SubAxisX";
        private const string SUB_AXIS_Y = "SubAxisY";

        public static void Init()
        {
            Input.AddState(A, Key.Space, 0);
            Input.AddState(B, Key.BackSpace, 2);
            Input.AddState(X, Key.X, 1);
            Input.AddState(Y, Key.Y, 3);
            Input.AddAxis(AXIS_X, Key.Right, Key.Left, StickAxis.LeftStickX);
            Input.AddAxis(AXIS_Y, Key.Up, Key.Down, StickAxis.LeftStickY);
            Input.AddAxis(SUB_AXIS_X, Key.D, Key.A, StickAxis.RightStickX);
            Input.AddAxis(SUB_AXIS_Y, Key.W, Key.S, StickAxis.RightStickY);
            //Input.AddTrigger("LTrigger", Key.O, Trigger.LeftTrigger);
            //Input.AddTrigger("RTrigger", Key.P, Trigger.RightTrigger);
        }

        public static bool ButtonA() => Input.GetState(A);
        public static bool ButtonB() => Input.GetState(B);
        public static bool ButtonX() => Input.GetState(X);
        public static bool ButtonY() => Input.GetState(Y);
        public static bool DownA() => Input.GetStateDown(A);
        public static bool DownB() => Input.GetStateDown(B);
        public static bool DownX() => Input.GetStateDown(X);
        public static bool DownY() => Input.GetStateDown(Y);
        public static Vector2 Axis() => new Vector2(Input.GetAxis(AXIS_X), Input.GetAxis(AXIS_Y));
        public static Vector2 SubAxis() => new Vector2(Input.GetAxis(SUB_AXIS_X), Input.GetAxis(SUB_AXIS_Y));
    }
}
