using System;
using System.Reflection;
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
            using var form = new LauncherForm();
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";
            form.Text = $"Miniscuplter Launcher v{version}";
            Application.Run(form);
        }
        finally
        {
            OwnedChildProcessJob.Dispose();
        }
    }
}
