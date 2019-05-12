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
            var result = Game.Run(600, 400, "Game", WindowStyle.FixedWindow, "icon.ico");
            if(result == GameExitResult.FailedInInitializingResource) {
                MessageBox.Show("Fatal Error (code:1)", "Error", MessageBoxType.Ok, MessageBoxIcon.Error);
            }
        }
    }
}
