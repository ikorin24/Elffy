#nullable enable
using System;
using System.Linq;
using Elffy;
using Elffy.Shape;
using Elffy.Threading;
using Elffy.UI;
using Elffy.Framing;
//using OpenTK;
using Elffy.Mathematics;
using System.Diagnostics;
using Elffy.InputSystem;
using System.Drawing;
using Elffy.Serialization;
using System.Reflection.Emit;
using Elffy.Effective;
using System.Runtime.InteropServices;
using System.Reflection;
using Elffy.Shading;

namespace ElffyGame
{
    public abstract class Scenario
    {
        public static void Start(IHostScreen screen)
        {
            screen.Camera.LookAt(new Vector3(0, 10, 0), new Vector3(40, 40, -40));
            new CameraMouse(screen.Camera, screen.Mouse, new Vector3(0, 0, 0)).Activate(screen.Layers.WorldLayer);
            Base.PmxModel.LoadResource("Alicia/Alicia_solid.pmx", screen.Layers.WorldLayer);
            return;

            var worldLayer = screen.Layers.WorldLayer;
            //screen.Light.GlobalAmbient = Color4.Red;

            //var light = new DirectLight();
            //light.Activate();
            var model = Resources.LoadModel("Alicia/Alicia_solid.pmx");

            var sw = new Stopwatch();
            sw.Start();
            //var shader = ShaderSource.Phong.Compile();
            model.Shader = ShaderSource.Normal;
            model.Activate(worldLayer);
            var c = new Cube();
            c.Shader = ShaderSource.Normal;
            c.Activate(worldLayer);

            var cc = new Cube() { Position = new Vector3(0, 10, 0), Material = Materials.RedPlastic, Shader = ShaderSource.Normal };
            cc.Activate(worldLayer);
            screen.Camera.LookAt(new Vector3(0, 10, 0), new Vector3(40, 40, -40));
            var cm = new CameraMouse(screen.Camera, screen.Mouse, new Vector3(0, 0, 0));
            cm.Activate(worldLayer);

            var cubeArray = Enumerable.Range(0, 9).Select(i => new Cube() { Position = new Vector3(3, 0, 0), Shader = ShaderSource.Normal }).ToArray();
            foreach(var cube in cubeArray) {
                cube.Rotate(Vector3.UnitY, 30f.ToRadian());
                cube.Rotate(Vector3.UnitZ, 10f.ToRadian());
            }
            for(int i = 1; i < cubeArray.Length; i++) {
                cubeArray[i - 1].Children.Add(cubeArray[i]);
                cubeArray[i].Activate(worldLayer);
            }
            cubeArray[0].Activate(worldLayer);
            sw.Stop();
            Debug.WriteLine(sw.ElapsedMilliseconds + "ms");

            //var light = new DirectLight();
            //light.Activate();
            //FrameProcess.WhileTrue(process =>
            //{
            //    if((int)(process.Time.TotalSeconds) % 2 == 0) {
            //        light.LightUp();
            //    }
            //    else {
            //        light.TurnOff();
            //    }
            //});

            //var cube = new Cube();
            //cube.Texture = await Texture.LoadFromAsync("cube.png");
            //cube.Position = new Vector3(0, 0, -25);

            //FrameProcess.WhileTrue(process =>
            //{
            //    cube.Rotate(new Vector3(1, 2, 3), MathHelper.Pi / 60 / 2);
            //});


            //var tex1 = Texture.LoadFrom("cube.png");
            //var tex2 = Texture.LoadFrom("sky.jpg");
            //var button = new Button(100, 100);
            //button.Texture = tex1;
            //button.KeyUp += (sender) =>
            //{
            //    System.Diagnostics.Debug.WriteLine("Go to Next Scenario");
            //    Scenario.GoToNext(new StartScenario());
            //};
            //button.MouseEnter += (sender, e) =>
            //{
            //    Debug.WriteLine("Mouse Enter");
            //    sender.Texture = tex2;
            //};
            //button.MouseLeave += (sender, e) =>
            //{
            //    sender.Texture = tex1;
            //};
            //Engine.CurrentScreen.UIRoot.Children.Add(button);

            //var b2 = new Button(100, 100);
            //b2.Position = new Point(100, 100);
            //b2.Texture = Texture.LoadFrom("cube.png");
            //Engine.CurrentScreen.UIRoot.Children.Add(b2);

            //FrameProcess.Begin(TimeSpan.FromSeconds(2), process =>
            //{
            //    b2.PositionX += 1;
            //    b2.PositionY += 1;
            //});

            //FrameProcess.WhileTrue(process => b2.IsVisible = (int)process.Time.TotalSeconds % 2 == 0);

            //var cubes = Enumerable.Range(0, 90).Select(i => new Cube() { Texture = cube.Texture }).ToArray();
            //for(int i = 0; i < cubes.Length; i++) {
            //    cubes[i].Position = new Vector3(1, 0.1f, 0);
            //    cubes[i].Rotate(Vector3.UnitY, 8f.ToRadian());
            //    cubes[i].Activate();
            //}

            //for(int i = 0; i < cubes.Length; i++) {
            //    if(i == 0) {
            //        cube.Children.Add(cubes[i]);
            //    }
            //    else {
            //        cubes[i - 1].Children.Add(cubes[i]);
            //    }
            //    if(i == cubes.Length - 1) {
            //        cubes[i].Rotate(Vector3.UnitX, 45f.ToRadian());
            //    }
            //}

            //cube.Activate();

            //Engine.CurrentScreen.Camera.LookAt(Vector3.Zero, new Vector3(0, 0, 10));

            ////cube.IsVisible = false;
            //var a = new Plain();
            //a.Position = new Vector3(0, 0, 0);
            //var sprite = Sprite.LoadFrom("TestSprite.xml");
            //sprite.PageChangingAlgorithm = () =>
            //{
            //    return (int)Engine.CurrentScreen.FrameNum / 3 % sprite.PageCount;
            //};
            //a.Texture = sprite;
            //a.Texture = Texture.LoadFrom("cube.png");
            //a.Activate();

            //using(var fragShader = FragmentShader.LoadFromResource("TestFragShader.frag"))
            //using(var vertShader = VertexShader.LoadFromResource("TestVertShader.vert")) {
            //    a.Shader = ShaderProgram.Create(vertShader, fragShader);
            //}

            //FrameProcess.WhileTrue(process =>
            //{
            //    var r = MathTool.TwoPi * (float)process.Time.TotalSeconds / 5;
            //    var rot = new Matrix3(MathTool.Cos(r), MathTool.Sin(r), 0,
            //                          -MathTool.Sin(r), MathTool.Cos(r), 0,
            //                          0, 0, 1);
            //    a.Position = new Vector3(1, 0, 0) * rot;
            //});

            //a.Shader = ShaderProgram.Default;

            //FrameProcess.WhileTrue(process =>
            //{
            //    var fovy = 25f + 15f * MathTool.Sin((float)process.Time.TotalSeconds / 2 * MathTool.TwoPi);
            //    Engine.CurrentScreen.Camera.ChangeFovy(fovy.ToRadian(), a.Position);
            //});

            //FrameProcess.WhileTrue(_ =>
            //{
            //    var mouse = Game.Mouse;
            //    var pos = mouse.Position;
            //    var wheel = mouse.Wheel();
            //    var l = mouse.IsDown(MouseButton.Left)   ? 'd' : mouse.IsUp(MouseButton.Left)   ? 'u' : mouse.IsPressed(MouseButton.Left)   ? '|' : ' ';
            //    var r = mouse.IsDown(MouseButton.Right)  ? 'd' : mouse.IsUp(MouseButton.Right)  ? 'u' : mouse.IsPressed(MouseButton.Right)  ? '|' : ' ';
            //    var m = mouse.IsDown(MouseButton.Middle) ? 'd' : mouse.IsUp(MouseButton.Middle) ? 'u' : mouse.IsPressed(MouseButton.Middle) ? '|' : ' ';

            //    var onscreen = mouse.OnScreen ? "in " : "out";
            //    Debug.WriteLine($"{onscreen}, ({pos.X:000}, {pos.Y:000}), {wheel}, [{l},{m},{r}]");
            //});
        }
    }
}
