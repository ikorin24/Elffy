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

namespace ElffyGame
{
    public class Game
    {
        private ElffyWindow _window;
        private List<GameObject> _gameObjectList = new List<GameObject>();

        public static Game Instance { get; private set; }
        public Size ClientSize => _window.ClientSize;

        private Game()
        {
        }

        public static void Run()
        {
            if(Instance != null) { throw new InvalidOperationException("Game is already Running"); }
            var game = new Game();
            Instance = game;
            using(var window = new ElffyWindow(800, 600, "Game", WindowStyle.FixedWindow)) {
                game._window = window;
                window.Initialize += game.Initialize;
                window.FrameRendering += game.FrameRendering;
                window.Closed += game.Closed;
                window.Run();
            }
        }

        public static void AddGameObject(Text gameObject)
        {
            if(gameObject == null) { return; }
            Instance._gameObjectList.Add(gameObject);
        }

        public static void RemoveGameObject(Text gameObject)
        {
            if(gameObject == null) { return; }
            Instance._gameObjectList.Remove(gameObject);
            gameObject.Dispose();
        }

        private void Initialize(object sender, EventArgs e)
        {
            // 入力の登録
            Input.AddState("A", Key.Space, 0);
            Input.AddState("B", Key.BackSpace, 2);
            Input.AddState("X", Key.X, 1);
            Input.AddState("Y", Key.Y, 3);
            Input.AddAxis("AxisX", Key.Right, Key.Left, StickAxis.LeftStickX);
            Input.AddAxis("AxisY", Key.Up, Key.Down, StickAxis.LeftStickY);

            // UIサンプル
            var text = new Text(Instance.ClientSize.Width, Instance.ClientSize.Height);
            text.Clear(Color.BlueViolet);
            AddGameObject(text);
        }

        private void FrameRendering(FrameEventArgs e)
        {
            DebugManager.Append($"FPS:{FPSManager.GetFPS():N1}");

            if(Input.GetStateDown("A")) {
                DebugManager.Append("A");
                (_gameObjectList.First() as Text).DrawString("ikorin24", new Font(FontFamily.GenericSerif, 24), Brushes.White, new Point());
            }
            if(Input.GetStateDown("B")) {
                DebugManager.Append("B");
            }
            if(Input.GetStateDown("X")) {
                DebugManager.Append("X");
            }
            if(Input.GetStateDown("Y")) {
                DebugManager.Append("Y");
            }

            foreach(var gameObject in _gameObjectList) {
                gameObject.Render();
            }
        }

        private void Closed(object sender, EventArgs e)
        {
            // リソースの解放
            foreach(var item in _gameObjectList) {
                item.Dispose();
            }
        }
    }
}
