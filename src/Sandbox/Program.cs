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
            SingleScreenApp.Start(1200, 675, "Sandbox", GameStarter.Start);
        }
    }
}
