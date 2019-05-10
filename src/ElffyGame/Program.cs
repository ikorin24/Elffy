using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            Game.Initialize += GameProgress.Initialize;
            //Game.Run(600, 400, "Game", WindowStyle.FixedWindow, ResourcePassword.GetPassword(), "icon.ico");
            var result = Game.Run(600, 400, "Game", WindowStyle.FixedWindow, ResourcePassword.GetPassword());
            if(result == GameExitResult.FailedInInitializingResource) {
                MessageBox.Show("Fatal Error (code:1)", "Error", MessageBoxType.Ok, MessageBoxIcon.Error);
            }
        }
    }
}
