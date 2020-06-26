#nullable enable
using System;
using System.Diagnostics;
using Elffy;
using Elffy.Core;
using Elffy.Games;
using Elffy.Platforms.Windows;
using Elffy.UI;
using Sandbox;

namespace Sandbox
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            SingleScreenApp.Start(GameStarter.Start);
        }
    }
}
