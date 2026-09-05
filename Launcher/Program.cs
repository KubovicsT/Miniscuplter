using System;
using System.Windows.Forms;

namespace Miniscuplter.Launcher;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        OwnedChildProcessJob.Initialize();
        try
        {
            var form = new LauncherForm { Text = "Miniscuplter Launcher v1.0" };
            Application.Run(form);
        }
        finally
        {
            OwnedChildProcessJob.Dispose();
        }
    }
}
