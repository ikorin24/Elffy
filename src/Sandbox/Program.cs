#nullable enable
using System;
using Elffy;
using Elffy.Diagnostics;

namespace Sandbox
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try {
                DevEnv.Run();
                Game.Start(1200, 675, "Sandbox", "icon.ico", GameStarter.Start);
            }
            finally {
                DevEnv.Stop();
            }
        }
    }
}
