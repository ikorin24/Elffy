﻿using Elffy;
using Elffy.InputSystem;
using Elffy.Core;
using Elffy.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
//using OpenTK;
using Elffy.Framing;
using Elffy.Shape;
//using OpenTK.Graphics;
using Elffy.Serialization;

namespace ElffyGame
{
    public class GameStartUp
    {
        public static void Initialize(object sender, EventArgs e)
        {
            //var loaded = SaveData.Default.Load();
            //if(loaded == false) {
            //    var block = new SaveBlock() { Name = "Test" };
            //    SaveData.Default.Blocks.Add(block);
            //    SaveData.Default.Save();
            //}

            //// 入力の登録
            //Controller.Init();
            //// ---------------------------------------

            //Light.CreateDirectLight(new Vector3(-1, 1, 1), Color4.White);
            ////Light.IsEnabled = true;

            //var cube = new Cube();
            //cube.Texture = new Texture("cube.png");
            //cube.Position = new Vector3(0, 0, 0);
            ////cube.MultiplyScale(2, 1, 1);
            //cube.Activate();

            //var xyCanvas = new Canvas(500, 500);
            //xyCanvas.ScaleX = 4f;
            //xyCanvas.ScaleY = 4f;
            //xyCanvas.Clear(Color.FromArgb(0, Color.Blue));
            //var pen = new Pen(Brushes.Red, 13f);
            //var points = new Point[] {
            //    new Point(250, 250), new Point(450, 250), new Point(430, 240),
            //};
            //xyCanvas.DrawLines(pen, points);
            //xyCanvas.DrawString("X", new Font(FontFamily.GenericSansSerif, 20), Brushes.Red, new PointF(430, 190));
            //pen = new Pen(Brushes.Green, 13f);
            //points = new Point[] {
            //    new Point(250, 250), new Point(250, 50), new Point(240, 70),
            //};
            //xyCanvas.DrawLines(pen, points);
            //xyCanvas.DrawString("Y", new Font(FontFamily.GenericSansSerif, 20), Brushes.Green, new PointF(200, 20));
            //xyCanvas.Activate();

            //var xzCanvas = new Canvas(500, 500);
            //xzCanvas.Rotate(Quaternion.FromAxisAngle(new Vector3(1, 0, 0), -MathHelper.PiOver2));
            //xzCanvas.ScaleX = 4f;
            //xzCanvas.ScaleY = 4f;
            //xzCanvas.Clear(Color.FromArgb(0, Color.Blue));
            //pen = new Pen(Brushes.Blue, 13f);
            //points = new Point[] {
            //    new Point(250, 250), new Point(250, 450), new Point(240, 440),
            //};
            //xzCanvas.DrawLines(pen, points);
            //xzCanvas.DrawString("Z", new Font(FontFamily.GenericSansSerif, 20), Brushes.Blue, new PointF(200, 440));
            //xzCanvas.Activate();


            //var canvas = new Canvas(300, 300);
            //var font = new Font(FontFamily.GenericSansSerif, 50);
            //var rand = new Random();
            //FrameProcess.While(() => true, _ => {
            //    //pen.Color = Rand.Color();
            //    //canvas.DrawLine(pen, rand.Next(canvas.PixelWidth), rand.Next(canvas.PixelHeight), rand.Next(canvas.PixelWidth), rand.Next(canvas.PixelHeight));
            //});
            //canvas.Clear(Color.White);
            //canvas.DrawString("test", font, Brushes.Green, new Point());
            //canvas.Position = new Vector3(2, 2, 1);
            //canvas.Activate();

            ////Camera.Current.Position = new Vector3(20f, 12f, 30f);
            //Camera.Current.Position = new Vector3(30f, 2.5f, 20f);
            //Camera.Current.Direction = Vector3.Zero - Camera.Current.Position;

            //FrameProcess.While(() => true, info => {
            //    var theta = MathHelper.TwoPi * info.FrameNum / 500;
            //    var cos = (float)Math.Cos(theta);
            //    var sin = (float)Math.Sin(theta);
            //    cube.Rotate(Quaternion.FromAxisAngle(new Vector3(1, 1, 0), 1f / 180 * MathHelper.Pi));
            //});

            //FrameProcess.While(() => true, info => {
            //    DebugManager.AppendIf(Controller.DownA(), "A");
            //    DebugManager.AppendIf(Controller.DownB(), "B");
            //});
            //var plain = new Plain();
            //plain.Position = new Vector3(1, 1, 0.04f);
            //plain.Activate();
            //MainCamera.Init();

            //FrameProcess.Begin(3000, anim => {
            //    canvas.Clear(Rand.Color());
            //    canvas.DrawString((anim.Time / 1000).ToString(), font, Brushes.Red, new PointF());
            //})
            //.Wait(2000)
            //.Begin(3000, anim => {
            //    canvas.Clear(Rand.Color());
            //    canvas.DrawString((anim.Time / 1000).ToString(), font, Brushes.Red, new PointF());
            //})
            //.Do(_ => {
            //    canvas.Clear(Color.White);
            //});

            //cube.IsVisible = false;
            //canvas.IsVisible = false;
            //plain.IsVisible = false;
            //xyCanvas.IsVisible = false;
            //xzCanvas.IsVisible = false;

            //for(int i = 0; i < 5; i++) {
            //    var dice = Resources.LoadModel("Dice.fbx");
            //    //var dice = new Cube();
            //    dice.Position = new Vector3(Rand.Float(-3f, 3f), Rand.Float(-3f, 3f), Rand.Float(-3f, 3f));
            //    dice.Activate();
            //    FrameProcess.WhileTrue(info => {
            //        dice.Rotate(Vector3.UnitY, MathHelper.Pi * Game.RenderDelta / 1000);
            //        dice.EnableVertexColor = (info.Time / 1000) % 2 == 0;
            //    });
            //}
            //FrameProcess.WhileTrue(frame => DebugManager.AppendIf(frame.FrameNum % 10 == 0, FPSManager.GetFPS()));
            //var sky = new Sky(1000);
            //sky.Texture = new Texture("sky.jpg");
            //sky.Activate();
            //FrameProcess.WhileTrue(frame => {
            //    var camPos = Camera.Current.Position;
            //    sky.PositionX = camPos.X;
            //    sky.PositionZ = camPos.Z;
            //});
        }
    }
}