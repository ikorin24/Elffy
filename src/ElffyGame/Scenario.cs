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
            cube.Position = new Vector3(0, 0, 0);
            cube.Activate(Game.Layers.WorldLayer);

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

            FrameProcess.Begin(2000, process =>
            {
                b2.Position += new Vector2(1, 1);
            });

            //FrameProcess.WhileTrue(process => System.Diagnostics.Debug.WriteLine($"{Game.Mouse.Position}, {Game.Mouse.OnScreen}"));
        }
    }
}
