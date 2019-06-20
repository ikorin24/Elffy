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
    public class GameStartUp
    {
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

            // ---------------------------------------

            //Light.CreateDirectLight(new Vector3(-1, 0, -3), Color4.White);
            var canvas = new Canvas2(300, 300) { Position = new Vector3(0, 0, -5) };
            canvas.Clear(Color.Blue);
            //canvas.DrawString("test", new Font(FontFamily.GenericSansSerif, 20), Brushes.Green, new PointF());
            //canvas.Test();
            //canvas.Activate();
            var a = Game.CurrentFrame;
            var font = new Font(FontFamily.GenericSerif, 20);
            //Animation.Create().Wait(1).Do(f => {
            //    DebugManager.Append($"test @ {Game.CurrentFrame}");
            //});


            var cube = new Cube();
            //cube.Position = new Vector3(2, 2, -3);
            cube.Position = new Vector3(0, 0, -5);

            //Animation.Create().While(() => true, info => {
            //    cube.Rotate(Quaternion.FromAxisAngle(new Vector3(1, 2, 3), 1f / 180 * MathHelper.Pi));
            //    cube.Rotate(Quaternion.FromAxisAngle(new Vector3(0, 0, 1), 0.8f / 180 * MathHelper.Pi));
            //});
            //Animation.Create().While(() => true, info => {
            //    var pos = cube.Position;
            //    cube.Position = new Vector3((info.FrameNum % 60) / 10f - 4, ((info.FrameNum + 15) % 80) / 10f - 4, pos.Z);
            //});
            cube.Activate();

            Animation.Create().While(() => true, info => {
                //if(Input.GetAxis(Controller.AXIS_X))
                Camera.Current.Position = Camera.Current.Position + new Vector3(Input.GetAxis(Controller.AXIS_X) * 0.1f, 0, 0);
                DebugManager.Append(Camera.Current.Position);
            });
        }
    }
}
