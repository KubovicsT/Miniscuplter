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
            Application.Run(new LauncherForm());
        }
        finally
        {
            OwnedChildProcessJob.Dispose();
        }
    }
}
