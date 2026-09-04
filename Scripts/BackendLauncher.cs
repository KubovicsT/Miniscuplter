using Godot;
using System;
using System.IO;

namespace Miniscuplter;

public partial class BackendLauncher : Node
{
    int _pid = -1;

    public override void _Ready()
    {
        string root = Environment.GetEnvironmentVariable("MINISCULPTER_ROOT") ?? "";
        if (string.IsNullOrWhiteSpace(root))
        {
            string exe = OS.GetExecutablePath();
            root = string.IsNullOrWhiteSpace(exe) ? ProjectSettings.GlobalizePath("res://") : Path.GetDirectoryName(exe) ?? ProjectSettings.GlobalizePath("res://");
        }

        string[] backendCandidates =
        {
            Path.Combine(root, "ai_backend", "app.py"),
            Path.Combine(root, "App", "ai_backend", "app.py"),
            ProjectSettings.GlobalizePath("res://ai_backend/app.py")
        };
        string? app = Array.Find(backendCandidates, File.Exists);
        if (app == null) { GD.Print("AI backend files were not found; editor remains usable without AI."); return; }

        string[] pythonCandidates =
        {
            Path.Combine(root, "Runtime", "Python", "python.exe"),
            Path.Combine(root, ".venv", "Scripts", "python.exe"),
            Path.Combine(root, "App", ".venv", "Scripts", "python.exe"),
            Path.Combine(Path.GetDirectoryName(app)!, ".venv", "Scripts", "python.exe")
        };
        string python = Array.Find(pythonCandidates, File.Exists) ?? "python";
        try
        {
            _pid = OS.CreateProcess(python, new[] { app });
            GD.Print(_pid > 0 ? $"AI backend launched (PID {_pid}) from {app}." : "Could not auto-launch AI backend; editor will remain usable without AI.");
        }
        catch (Exception ex)
        {
            GD.PrintErr("AI backend auto-launch failed: " + ex.Message);
        }
    }

    public override void _ExitTree()
    {
        if (_pid > 0)
        {
            try { OS.Kill(_pid); } catch { }
        }
    }
}
