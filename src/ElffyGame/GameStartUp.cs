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
using Elffy.Serialization;

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

            //Light.CreateDirectLight(new Vector3(-1, -1, -1), Color4.White);

            var cube = new Cube();
            cube.Texture = new Texture("cube.png");
            cube.Position = new Vector3(0, 0, 0);
            //cube.MultiplyScale(2, 1, 1);
            cube.Activate();

            var xyCanvas = new Canvas(500, 500);
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

            var xzCanvas = new Canvas(500, 500);
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


            var canvas = new Canvas(300, 300);
            var font = new Font(FontFamily.GenericSansSerif, 50);
            var rand = new Random();
            Animation.While(() => true, _ => {
                //pen.Color = Rand.Color();
                //canvas.DrawLine(pen, rand.Next(canvas.PixelWidth), rand.Next(canvas.PixelHeight), rand.Next(canvas.PixelWidth), rand.Next(canvas.PixelHeight));
            });
            canvas.Clear(Color.White);
            canvas.DrawString("test", font, Brushes.Green, new Point());
            canvas.Position = new Vector3(2, 2, 1);
            canvas.Activate();

            Camera.Current.Position = new Vector3(20f, 12f, 30f);
            Camera.Current.Direction = Vector3.Zero - Camera.Current.Position;

            Animation.While(() => true, info => {
                var theta = MathHelper.TwoPi * info.FrameNum / 500;
                var cos = (float)Math.Cos(theta);
                var sin = (float)Math.Sin(theta);
                cube.Rotate(Quaternion.FromAxisAngle(new Vector3(1, 1, 0), 1f / 180 * MathHelper.Pi));
            });

            Animation.While(() => true, info => {
                DebugManager.AppendIf(Controller.DownA(), "A");
                DebugManager.AppendIf(Controller.DownB(), "B");
            });
            var plain = new Plain();
            plain.Position = new Vector3(1, 1, 0.04f);
            plain.Activate();
            MainCamera.Init();

            Animation.Begin(3000, anim => {
                canvas.Clear(Rand.Color());
                canvas.DrawString((anim.Time / 1000).ToString(), font, Brushes.Red, new PointF());
            })
            .Wait(2000)
            .Begin(3000, anim => {
                canvas.Clear(Rand.Color());
                canvas.DrawString((anim.Time / 1000).ToString(), font, Brushes.Red, new PointF());
            })
            .Do(_ => {
                canvas.Clear(Color.White);
            });

            cube.IsVisible = false;
            canvas.IsVisible = false;
            plain.IsVisible = false;
            xyCanvas.IsVisible = false;
            xzCanvas.IsVisible = false;

            // TODO: Positionable の回転がおかしい。ワールド座標の軸に対して回転している

            for(int i = 0; i < 100; i++) {
                var dice = Resources.LoadModel("Dice.fbx");
                dice.Position = new Vector3(Rand.Float(-15f, 15f), Rand.Float(-15f, 15f), Rand.Float(-15f, 15f));
                dice.Activate();
                Animation.WhileTrue(info => {
                    dice.Rotate(Vector3.UnitY, MathHelper.Pi * Game.RenderDelta / 5000);
                });
            }
            //var dice = Resources.LoadModel("Dice2.fbx");
            //dice.Activate();
            //Animation.WhileTrue(info => {
            //    dice.Rotate(Vector3.UnitY, MathHelper.Pi * Game.RenderDelta / 5000);
            //});

            //var testCube = Resources.LoadModel("cube.fbx");
            //testCube.Activate();
            //var sky = new Sky(Camera.Current.Far * 0.5f);
            var sky = new Sky(50);
            //sky.Texture = new Texture("sky.jpg");
            sky.Activate();
        }
    }
}
