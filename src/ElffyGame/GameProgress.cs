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
using Elffy.Shape;
using OpenTK.Graphics;

namespace ElffyGame
{
    public class GameProgress : FrameObject
    {
        private Font _font = new Font(FontFamily.GenericSerif, 20);
        private Canvas _canvas;
        private Cube _cube;

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
            progress.Activate();
        }

        public override void Start()
        {
            //Light.CreateDirectLight(new Vector3(-1, 0, -3), Color4.White);
            //Light.CreateDirectLight(new Vector3(-1, 0, -3), Color4.Blue);
            //Light.CreateDirectLight(new Vector3(1, 0, 0), Color4.Yellow);

            _canvas = new Canvas(300, 300);
            _canvas.Position = new Vector3(0, 0, -5);
            _canvas.Clear(Color.Blue);
            _canvas.Activate();
            Animation.Create()
                     .While(() => true, info => {
                         _canvas.Clear(Rand.Color());
                         //_canvas.DrawString($"{FPSManager.GetFPS():N2}", _font, Brushes.Yellow, new Point());
                     });


            _cube = new Cube();
            //_cube.Texture = new Texture();

            //_cube.Material = new Material(new Color4(0.24725f, 0.1995f, 0.0225f, 1.0f), 
            //                              new Color4(0.75164f, 0.60648f, 0.22648f, 1.0f),
            //                              new Color4(0.628281f, 0.555802f, 0.366065f, 1.0f), 
            //                              50f);
            //_cube.Material = new Material();
            _cube.Position = new Vector3(2, 2, -9);
            Animation.Create().While(() => true, info => {
                var pos = _cube.Position;
                _cube.Position = new Vector3((info.FrameNum % 60) / 10f - 4, ((info.FrameNum + 15) % 80) / 10f - 4, pos.Z);
            });
            _cube.Activate();
            //_cube.IsVisible = false;
        }

        public override void Update()
        {
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
            //_canvas.Translate(0, 0, -z);

            //Input.PadDump();
        }
    }
}
