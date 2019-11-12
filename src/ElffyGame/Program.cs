using System;
using Elffy;
using Elffy.Core;
using Elffy.Platforms.Windows;

namespace ElffyGame
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            ProcessHelper.SingleLaunch(GameStart);
        }

        static void GameStart()
        {
            try {
                //Game.Initialize += GameStartUp.Initialize;
                Game.Initialized += sender => Scenario.Start(new StartScenario());
                Game.Run(800, 450, "Game", WindowStyle.Default, "icon.ico");
            }
            catch(Exception) {
                MessageBox.Show("Fatal Error (CODE : 0)", "Error", MessageBoxType.Ok, MessageBoxIcon.Error);
                // TODO: ロギング
                return;
            }
        }
    }
}
