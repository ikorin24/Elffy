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
    public class GameProgress : GameObject
    {
        private Font _font = new Font(FontFamily.GenericSerif, 20);
        private Text _text;

        public static void Initialize(object sender, EventArgs e)
        {
            // 入力の登録
            Input.AddState("A", Key.Space, 0);
            Input.AddState("B", Key.BackSpace, 2);
            Input.AddState("X", Key.X, 1);
            Input.AddState("Y", Key.Y, 3);
            Input.AddAxis("AxisX", Key.Right, Key.Left, StickAxis.LeftStickX);
            Input.AddAxis("AxisY", Key.Up, Key.Down, StickAxis.LeftStickY);

            var progress = new GameProgress();
            Game.AddGameObject(progress);
        }

        public override void Start()
        {
            _font = new Font(FontFamily.GenericSansSerif, 130);
            _text = new Text(Game.ClientSize);
            _text.Tag = "sampleText";
            _text.Clear(Color.Violet);
            Game.AddGameObject(_text);
        }

        public override void Update()
        {
            if(_text != null) {
                _text.Clear(Color.Violet);
                _text.DrawString($"{FPSManager.GetFPS():N2}", _font, Brushes.White, new Point());
            }


            //DebugManager.Append($"{FPSManager.GetFPS():N2}");
            if(Input.GetStateDown("A")) {
                DebugManager.Append("A");
            }
            if(Input.GetStateDown("B")) {
                DebugManager.Append("B");
            }
        }
    }
}
