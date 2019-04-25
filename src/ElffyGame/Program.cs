using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elffy;

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
            System.Diagnostics.Debug.WriteLine(ResourcePassword.GetPassword());
            Game.Initialize += GameProgress.Initialize;
            //Game.Run(600, 400, "Game", WindowStyle.FixedWindow, "password", "icon.ico");
            Game.Run(600, 400, "Game", WindowStyle.FixedWindow, "ikorin24pass");
        }
    }
}
