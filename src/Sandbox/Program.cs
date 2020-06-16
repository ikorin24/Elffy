#nullable enable
using System;
using System.Diagnostics;
using Elffy;
using Elffy.Core;
using Elffy.Platforms.Windows;
using Elffy.UI;

namespace ElffyGame
{
    static class Program
    {
        [STAThread]
        static void Main() => ProcessHelper.SingleLaunch(() =>
        {
            try {
                Resources.Initialize();
                Engine.Run();
                Engine.ShowScreen(1600, 900, "Game", Resources.LoadIcon("icon.ico"), WindowStyle.Default, YAxisDirection.TopToBottom, screen => Scenario.Start(screen));
            }
            catch(Exception ex) {
                Debug.WriteLine(ex);
                MessageBox.Show(ex.ToString(), "Error", MessageBoxType.Ok, MessageBoxIcon.Error);
                return;
            }
        });
    }
}
