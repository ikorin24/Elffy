using Elffy;
using Elffy.Input;
using Elffy.Core;
using Elffy.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using Elffy.Animation;

namespace ElffyGame
{
    public class GameProgress : GameObject
    {
        private Font _font = new Font(FontFamily.GenericSerif, 20);
        private Canvas _canvas;

        public static void Initialize(object sender, EventArgs e)
        {
            var loaded = SaveData.Default.Load();
            if(loaded == false) {
                var block = new SaveBlock() { Name = "Test" };
                SaveData.Default.Blocks.Add(block);
                SaveData.Default.Save();
            }

            // 入力の登録
            Input.AddState(Controller.A, Key.Space, 0);
            Input.AddState(Controller.B, Key.BackSpace, 2);
            Input.AddState(Controller.X, Key.X, 1);
            Input.AddState(Controller.Y, Key.Y, 3);
            Input.AddAxis(Controller.AXIS_X, Key.Right, Key.Left, StickAxis.LeftStickX);
            Input.AddAxis(Controller.AXIS_Y, Key.Up, Key.Down, StickAxis.LeftStickY);
            Input.AddAxis(Controller.SUB_AXIS_X, Key.D, Key.A, StickAxis.RightStickX);
            Input.AddAxis(Controller.SUB_AXIS_Y, Key.W, Key.S, StickAxis.RightStickY);

            Input.AddTrigger("LTrigger", Key.O, Trigger.LeftTrigger);
            Input.AddTrigger("RTrigger", Key.P, Trigger.RightTrigger);

            var progress = new GameProgress();
            Game.AddGameObject(progress);
        }

        Animation _animation;

        public override void Start()
        {
            _font = new Font(FontFamily.GenericSansSerif, 130);
            _canvas = new Canvas(Game.ClientSize);
            _canvas.Tag = "sampleText";
            _canvas.Position = new Vector3(0, 0, 0);
            //_canvas.MultiplyScale(20);
            Game.AddGameObject(_canvas);

            _animation = Animation.Create()
                                  .Begin(100, info => DebugManager.Append("a" + info.FrameNum.ToString()))
                                  .Wait(500)
                                  .Begin(100, info => DebugManager.Append("b" + info.FrameNum.ToString()))
                                  .While(() => !Input.GetState(Controller.A), info => DebugManager.Append("hoge" + info.FrameNum.ToString()))
                                  .Do(info => DebugManager.Append("Complete"));
            _animation.Cancel();
        }

        public override void Update()
        {
            //throw new Exception("hoge");
            if(Input.GetState(Controller.B)) {
                _animation.Cancel();
            }

            if(Game.RenderDelta >= 1d / 45) {
                _canvas.Clear(Color.Red);
            }
            else{
                _canvas.Clear(Color.Blue);
            }


            _canvas.DrawString($"{FPSManager.GetFPS():N2}", _font, Brushes.White, new Point());


            DebugManager.AppendIf(Input.GetStateDown(Controller.A), Controller.A);
            DebugManager.AppendIf(Input.GetStateDown(Controller.B), Controller.B);
            DebugManager.AppendIf(Input.GetStateDown(Controller.Y), Controller.Y);

            //DebugManager.Append($"Left Trigger : {Input.GetTrigger("LTrigger")}");
            //DebugManager.Append($"Right Trigger : {Input.GetTrigger("RTrigger")}");

            // https://blog.miz-ar.info/2017/12/opengl-projection-matrix/

            var p = 0.02f;
            var x = Input.GetAxis(Controller.AXIS_X) * p;
            var y = Input.GetAxis(Controller.AXIS_Y) * p;
            var z = -Input.GetAxis(Controller.SUB_AXIS_Y) * p;
            //DebugManager.Append(Camera.Current.P);
            //DebugManager.Append(Environment.NewLine);
            //DebugManager.Append(Camera.Current.Position);
            //DebugManager.Append(_canvas.Position);
            //DebugManager.Append(Environment.NewLine);
            //Camera.Move(x, y, z);
            Camera.Move(x, y, 0);
            _canvas.Translate(0, 0, -z);

            //Input.PadDump();
        }
    }
}
