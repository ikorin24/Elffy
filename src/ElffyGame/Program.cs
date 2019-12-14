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
                Engine.SingleScreenRun(800, 450, "Game", WindowStyle.Default, YAxisDirection.TopToBottom, "icon.ico", 
                                       _ => Scenario.Start(new StartScenario()));
            }
            catch(Exception ex) {
                Debug.WriteLine(ex);
                MessageBox.Show(ex.ToString(), "Error", MessageBoxType.Ok, MessageBoxIcon.Error);
                return;
            }
        }
    }
}
