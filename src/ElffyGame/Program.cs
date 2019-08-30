using System;
using Elffy;
using Elffy.Control;

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
                Game.Initialize += GameStartUp.Initialize;
                var result = Game.Run(800, 450, "Game", WindowStyle.FixedWindow, "icon.ico");
                if(result == GameExitResult.FailedInInitializingResource) {
                    MessageBox.Show("Fatal Error (CODE : 1)", "Error", MessageBoxType.Ok, MessageBoxIcon.Error);
                    return;
                }
            }
            catch(Exception) {
                MessageBox.Show("Fatal Error (CODE : 0)", "Error", MessageBoxType.Ok, MessageBoxIcon.Error);
                // TODO: ロギング
                return;
            }
        }
    }
}
