#nullable enable
using System;
using Elffy.Games;

namespace Sandbox
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Game.Start(1200, 675, "Sandbox", AssemblyInfo.IsDebug, GameStarter.Start);
        }
    }
}
