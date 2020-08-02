#nullable enable
using System;
using Elffy.Diagnostics;
using Elffy.Games;

namespace Sandbox
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try {
                if(AssemblyInfo.IsDebug) {
                    DiagnosticsSetting.Run();
                }
                Game.Start(1200, 675, "Sandbox", GameStarter.Start);
            }
            finally {
                DiagnosticsSetting.Stop();
            }
        }
    }
}
