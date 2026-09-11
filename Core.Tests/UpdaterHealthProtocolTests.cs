using System.Runtime.CompilerServices;

internal static class UpdaterHealthProtocolTests
{
    [ModuleInitializer]
    internal static void ValidateUpdaterHealthProtocol()
    {
        string root = Directory.GetCurrentDirectory();
        string updaterPath = Path.Combine(root, "Updater", "Program.cs");
        string launcherPath = Path.Combine(root, "Launcher", "Program.cs");
        if (!File.Exists(updaterPath) || !File.Exists(launcherPath))
            throw new InvalidOperationException("TEST FAILED: updater/launcher source files are missing");

        string updater = File.ReadAllText(updaterPath);
        string launcher = File.ReadAllText(launcherPath);

        int verifyStart = updater.IndexOf("static void VerifyLauncherStartup", StringComparison.Ordinal);
        int restartStart = updater.IndexOf("static void TryRestartRestoredLauncher", StringComparison.Ordinal);
        Assert(verifyStart >= 0 && restartStart > verifyStart, "launcher health validation method is missing");
        string verify = updater[verifyStart..restartStart];

        Assert(verify.Contains("bool healthConfirmed = false;", StringComparison.Ordinal), "health validation does not track confirmed startup");
        Assert(verify.Contains("healthConfirmed = true;", StringComparison.Ordinal), "health-token success does not mark the launcher healthy");
        Assert(verify.Contains("if (!healthConfirmed)", StringComparison.Ordinal), "healthy launcher is not protected from probe cleanup");
        Assert(verify.Contains("process.Kill(true);", StringComparison.Ordinal), "failed/timed-out launcher probe is not terminated before rollback");

        int successMark = verify.IndexOf("healthConfirmed = true;", StringComparison.Ordinal);
        int successReturn = verify.IndexOf("return;", successMark, StringComparison.Ordinal);
        int cleanupGuard = verify.IndexOf("if (!healthConfirmed)", StringComparison.Ordinal);
        Assert(successMark >= 0 && successReturn > successMark && cleanupGuard > successReturn,
            "successful health validation must preserve the running launcher before cleanup executes");

        Assert(updater.Contains("RollbackManagedUpdate(target, backup, parked, backupComplete, installStarted, preservedRestored);", StringComparison.Ordinal),
            "failed health validation no longer rolls back the managed update");
        Assert(Count(updater, "TryRestartRestoredLauncher(restart, target);") >= 2,
            "rollback paths no longer restart the restored launcher");

        Assert(launcher.Contains("--update-health-token", StringComparison.Ordinal), "launcher no longer accepts the updater health token");
        Assert(launcher.Contains("form.Shown +=", StringComparison.Ordinal) && launcher.Contains("File.WriteAllText(full", StringComparison.Ordinal),
            "launcher does not confirm health only after its UI is shown");
    }

    static int Count(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
