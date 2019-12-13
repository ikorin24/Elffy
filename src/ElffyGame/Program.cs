#nullable enable
using System;
using System.Diagnostics;
using Elffy;
using Elffy.Core;
using Elffy.Platforms.Windows;
using Elffy.UI;

namespace ElffyGame
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main() => ProcessHelper.SingleLaunch(GameStart);

        static void GameStart()
        {
            try {
                Game.Initialized += sender => Scenario.Start(new StartScenario());
                Game.Run(800, 450, "Game", WindowStyle.Default, YAxisDirection.TopToBottom, "icon.ico");
            }
            catch(Exception ex) {
                Debug.WriteLine(ex);
                MessageBox.Show("Fatal Error (CODE : 0)", "Error", MessageBoxType.Ok, MessageBoxIcon.Error);
                // TODO: ロギング
                return;
            }
        }
    }
}
