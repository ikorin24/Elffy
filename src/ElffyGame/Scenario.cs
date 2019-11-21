#nullable enable
using System;
using System.Linq;
using Elffy;
using Elffy.Shape;
using Elffy.Threading;
using Elffy.UI;
using Elffy.Framing;
using OpenTK;
using Elffy.Mathmatics;
using System.Diagnostics;
using Elffy.InputSystem;

namespace ElffyGame
{
    public abstract class Scenario
    {
        public static bool IsRunning => Current != null;

        public static Scenario? Current { get; private set; }

        public static void Start(Scenario scenario)
        {
            if(IsRunning) { throw new InvalidOperationException($"{nameof(Scenario)} is already running."); }
            if(scenario == null) { throw new ArgumentNullException(nameof(scenario)); }
            GoToNextPrivate(scenario);
        }

        public static void GoToNext(Scenario scenario)
        {
            if(!IsRunning) { throw new InvalidOperationException($"{nameof(Scenario)} is not running."); }
            if(scenario == null) { throw new ArgumentNullException(nameof(scenario)); }
            GoToNextPrivate(scenario);
        }

        private static void GoToNextPrivate(Scenario scenario)
        {
            Current = scenario;
            Dispatcher.Invoke(scenario.Start);
        }

        protected abstract void Start();
    }

    public class StartScenario : Scenario
    {
        protected override async void Start()
        {
            var light = new DirectLight();
            light.Activate();
            FrameProcess.WhileTrue(process =>
            {
                if((int)(process.Time.TotalSeconds) % 2 == 0) {
                    light.LightUp();
                }
                else {
                    light.TurnOff();
                }
            });

            var cube = new Cube();
            cube.Texture = await Texture.LoadFromAsync("cube.png");
            cube.Position = new Vector3(0, 0, -25);

            FrameProcess.WhileTrue(process =>
            {
                cube.Rotate(new Vector3(1, 2, 3), MathHelper.Pi / 60 / 2);
            });

            var button = new Button(100, 100);
            button.KeyUp += (sender) =>
            {
                System.Diagnostics.Debug.WriteLine("Go to Next Scenario");
                Scenario.GoToNext(new StartScenario());
            };
            button.MouseEnter += (sender, e) =>
            {
                Debug.WriteLine("Mouse Enter");
            };
            Game.UI.Add(button);

            var b2 = new Button(100, 100);
            b2.Position = new Vector2(100, 100);
            Game.UI.Add(b2);

            FrameProcess.Begin(TimeSpan.FromSeconds(2), process =>
            {
                b2.Position += new Vector2(1, 1);
            });

            var cubes = Enumerable.Range(0, 90).Select(i => new Cube() { Texture = cube.Texture }).ToArray();
            for(int i = 0; i < cubes.Length; i++) {
                cubes[i].Position = new Vector3(1, 0.1f, 0);
                cubes[i].Rotate(Vector3.UnitY, 8f.ToRadian());
                cubes[i].Activate(Game.Layers.WorldLayer);
            }

            for(int i = 0; i < cubes.Length; i++) {
                if(i == 0) {
                    cube.Children.Add(cubes[i]);
                }
                else {
                    cubes[i - 1].Children.Add(cubes[i]);
                }
                if(i == cubes.Length - 1) {
                    cubes[i].Rotate(Vector3.UnitX, 45f.ToRadian());
                }
            }

            cube.Activate();

            var camera = Game.Camera;
            Game.Camera.LookAt(Vector3.Zero, new Vector3(0, 0, 10));

            //cube.IsVisible = false;
            var a = new Plain();
            a.Position = new Vector3(0, 0, 0);
            var sprite = Sprite.LoadFrom("TestSprite.xml");
            sprite.PageChangingAlgorithm = () =>
            {
                return (int)Game.FrameNum / 3 % sprite.PageCount;
            };
            a.Texture = sprite;
            a.Texture = Texture.LoadFrom("cube.png");
            a.Activate();

            using(var fragShader = FragmentShader.LoadFromResource("TestFragShader.frag"))
            using(var vertShader = VertexShader.LoadFromResource("TestVertShader.vert")) {
                a.Shader = ShaderProgram.Create(vertShader, fragShader);
            }

            FrameProcess.WhileTrue(process =>
            {
                var r = MathTool.TwoPi * (float)process.Time.TotalSeconds / 5;
                var rot = new Matrix3(MathTool.Cos(r), MathTool.Sin(r), 0,
                                      -MathTool.Sin(r), MathTool.Cos(r), 0,
                                      0, 0, 1);
                a.Position = new Vector3(1, 0, 0) * rot;
            });

            a.Shader = ShaderProgram.Default;

            FrameProcess.WhileTrue(process =>
            {
                var fovy = 25f + 15f * MathTool.Sin((float)process.Time.TotalSeconds / 2 * MathTool.TwoPi);
                Game.Camera.ChangeFovy(fovy.ToRadian(), a.Position);
            });

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
