using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Elffy;
using Elffy.Shape;
using Elffy.Threading;
using Elffy.UI;
using Elffy.Framing;
using OpenTK;
using Elffy.Input;
using Elffy.Mathmatics;

namespace ElffyGame
{
    public abstract class Scenario
    {
        public static bool IsRunning => Current != null;

        public static Scenario Current { get; private set; }

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
        protected override void Start()
        {
            var cube = new Cube();
            cube.Texture = new Texture("cube.png");
            cube.Position = new Vector3(10, 0, 0);
            //cube.Activate(Game.Layers.WorldLayer);

            FrameProcess.WhileTrue(process =>
            {
                cube.Rotate(new Vector3(1, 2, 3), MathHelper.Pi / 60 / 2);
            });

            var button = new Button(100, 100);
            button.KeyUp += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine("Go to Next Scenario");
                Scenario.GoToNext(new StartScenario());
            };
            Game.UIRoot.Children.Add(button);

            var b2 = new Button(100, 100);
            b2.Position = new Vector2(100, 100);
            Game.UIRoot.Children.Add(b2);

            FrameProcess.Begin(TimeSpan.FromSeconds(2), process =>
            {
                b2.Position += new Vector2(1, 1);
            });

            //FrameProcess.WhileTrue(process => System.Diagnostics.Debug.WriteLine($"{Game.Mouse.Position}, {Game.Mouse.OnScreen}"));

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

            cube.Activate(Game.Layers.WorldLayer);

            var camera = Game.Camera;
            camera.Position = new Vector3(30, 30, 30);
            camera.Direction = -camera.Position;
        }
    }
}
