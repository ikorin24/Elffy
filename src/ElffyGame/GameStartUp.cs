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
            Controller.Init();
            // ---------------------------------------

            Light.CreateDirectLight(new Vector3(-1, -1, -1), Color4.White);

            var cube = new Cube();
            cube.Texture = new Texture("cube.png");
            cube.Position = new Vector3(0, 0, 0);
            //cube.MultiplyScale(2, 1, 1);
            cube.Activate();

            var xyCanvas = new Canvas2(500, 500);
            xyCanvas.ScaleX = 4f;
            xyCanvas.ScaleY = 4f;
            xyCanvas.Clear(Color.FromArgb(0, Color.Blue));
            var pen = new Pen(Brushes.Red, 13f);
            var points = new Point[] {
                new Point(250, 250), new Point(450, 250), new Point(430, 240),
            };
            xyCanvas.DrawLines(pen, points);
            xyCanvas.DrawString("X", new Font(FontFamily.GenericSansSerif, 20), Brushes.Red, new PointF(430, 190));
            pen = new Pen(Brushes.Green, 13f);
            points = new Point[] {
                new Point(250, 250), new Point(250, 50), new Point(240, 70),
            };
            xyCanvas.DrawLines(pen, points);
            xyCanvas.DrawString("Y", new Font(FontFamily.GenericSansSerif, 20), Brushes.Green, new PointF(200, 20));
            xyCanvas.Activate();

            var xzCanvas = new Canvas2(500, 500);
            xzCanvas.Rotate(Quaternion.FromAxisAngle(new Vector3(1, 0, 0), -MathHelper.PiOver2));
            xzCanvas.ScaleX = 4f;
            xzCanvas.ScaleY = 4f;
            xzCanvas.Clear(Color.FromArgb(0, Color.Blue));
            pen = new Pen(Brushes.Blue, 13f);
            points = new Point[] {
                new Point(250, 250), new Point(250, 450), new Point(240, 440),
            };
            xzCanvas.DrawLines(pen, points);
            xzCanvas.DrawString("Z", new Font(FontFamily.GenericSansSerif, 20), Brushes.Blue, new PointF(200, 440));
            xzCanvas.Activate();

            Camera.Current.Position = new Vector3(6f, 5f, 7f);
            //Camera.Current.Position = new Vector3(0, 7, 0.01f);
            Camera.Current.Direction = Vector3.Zero - Camera.Current.Position;

            Animation.Create().While(() => true, info => {
                var theta = MathHelper.TwoPi * info.FrameNum / 500;
                var cos = (float)Math.Cos(theta);
                var sin = (float)Math.Sin(theta);
                //Camera.Current.Position = new Vector3(5 * cos, 5, 5 * (-sin));
                //Camera.Current.Direction = Vector3.Zero - Camera.Current.Position;
                cube.Rotate(Quaternion.FromAxisAngle(new Vector3(1, 1, 0), 1f / 180 * MathHelper.Pi));
            });

            Animation.Create().While(() => true, info => {
                DebugManager.AppendIf(Controller.DownA(), "A");
                DebugManager.AppendIf(Controller.DownB(), "B");
            });
        }
    }
}
