using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy;
using Elffy.Shape;
using Elffy.Threading;
using Elffy.UI;
using OpenTK;

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
            GameThread.Invoke(scenario.Start);
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
            cube.Activate();

            var button = new Button(100, 100);
            button.KeyUp += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine("Go to Next Scenario");
                Scenario.GoToNext(new StartScenario());
            };
            Game.UI.RootChildren.Add(button);

            var b2 = new Button(100, 100);
            b2.Position = new Vector2(100, 100);
            Game.UI.RootChildren.Add(b2);
        }
    }
}
