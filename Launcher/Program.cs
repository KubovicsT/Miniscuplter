using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Miniscuplter.Launcher;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        OwnedChildProcessJob.Initialize();
        try
        {
            using var form = new LauncherForm();
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "unknown";
            form.Text = $"Miniscuplter Launcher v{version}";

            string? healthToken = ArgumentValue(args, "--update-health-token");
            if (!string.IsNullOrWhiteSpace(healthToken))
            {
                form.Shown += (_, _) =>
                {
                    try
                    {
                        string full = Path.GetFullPath(healthToken);
                        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                        File.WriteAllText(full, $"healthy {version} {DateTime.UtcNow:O}");
                    }
                    catch { }
                };
            }

            Application.Run(form);
        }
        finally
        {
            OwnedChildProcessJob.Dispose();
        }
    }

    static string? ArgumentValue(string[] args, string name)
    {
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return null;
    }
}
