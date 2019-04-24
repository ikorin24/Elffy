using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elffy;
using Elffy.Core;

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
            //Game.Run(600, 400, "Game", WindowStyle.FixedWindow, "password", "icon.ico");
            Game.Run(600, 400, "Game", WindowStyle.FixedWindow, "ikorin24pass");
        }
    }
}
