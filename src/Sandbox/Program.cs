#nullable enable
using System;
using Elffy;
using Elffy.Diagnostics;

[assembly: GenerateResourceFile("Resources", "Resources.dat")]

namespace Sandbox
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try {
                DevEnv.Run();
                Game.Start(1200, 675, "Sandbox", "icon.ico", Startup.Start);
            }
            finally {
                DevEnv.Stop();
            }
        }
    }
}
