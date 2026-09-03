using Godot;
using System;
using System.IO;

namespace Miniscuplter;

public partial class BackendLauncher : Node
{
    int _pid = -1;

    public override void _Ready()
    {
        string app = ProjectSettings.GlobalizePath("res://ai_backend/app.py");
        if (!File.Exists(app)) return;

        string venv = ProjectSettings.GlobalizePath("res://ai_backend/.venv/Scripts/python.exe");
        string python = File.Exists(venv) ? venv : "python";
        try
        {
            _pid = OS.CreateProcess(python, new[] { app });
            GD.Print(_pid > 0 ? $"AI backend launched (PID {_pid})." : "Could not auto-launch AI backend; editor will remain usable without AI.");
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
