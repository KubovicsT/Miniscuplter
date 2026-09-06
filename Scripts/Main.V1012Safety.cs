using Godot;
using System;
using System.IO;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    Timer? _v1012AutosaveTimer;

    public void InstallV1012SafetyBridge()
    {
        // Install after the historical UI composition is complete. This makes the newest
        // user-facing entry points use the same guarded implementations as the stabilized
        // project/export paths instead of bypassing them through older handlers.
        ReplaceV1012Button("Export Selected STL", OpenV099SafeExportDialog);
        ReplaceV095Button("Export STL", OpenV099SafeExportDialog);
        ReplaceV095Button("Save Project", OpenV099ProjectSaveDialog);
        ReplaceV095Button("Load Project", OpenV099ProjectLoadDialog);

        if (_v055AutosaveTimer != null && GodotObject.IsInstanceValid(_v055AutosaveTimer))
        {
            _v055AutosaveTimer.Stop();
            _v055AutosaveTimer.QueueFree();
            _v055AutosaveTimer = null;
        }

        _v1012AutosaveTimer = new Timer { WaitTime = 120, OneShot = false, Autostart = true };
        _v1012AutosaveTimer.Timeout += AutosaveV1012;
        AddChild(_v1012AutosaveTimer);
    }

    void ReplaceV1012Button(string text, Action action)
    {
        var buttons = FindChildren("*", "Button", true, false)
            .OfType<Button>()
            .Where(b => b.Visible && b.Text == text)
            .ToList();

        foreach (var old in buttons)
        {
            if (old.GetParent() is not Container parent) continue;
            int index = old.GetIndex();
            old.Visible = false;
            old.Disabled = true;
            var replacement = new Button
            {
                Text = text,
                TooltipText = old.TooltipText,
                SizeFlagsHorizontal = old.SizeFlagsHorizontal
            };
            replacement.Pressed += action;
            parent.AddChild(replacement);
            parent.MoveChild(replacement, index);
        }
    }

    void AutosaveV1012()
    {
        if (_objects.Count == 0) return;
        try
        {
            string recovery = Path.Combine(V099ProjectRoot(), "Recovery");
            Directory.CreateDirectory(recovery);
            string path = Path.Combine(recovery, "autosave.msculpt");
            SafeV095SaveProject(path);
            if (_v055JobStatus != null) _v055JobStatus.Text = "Recovery autosave updated through the guarded project writer.";
        }
        catch (Exception ex)
        {
            if (_v055JobStatus != null) _v055JobStatus.Text = "Recovery autosave failed: " + ex.Message;
            SetStatus("Recovery autosave failed without replacing the previous recovery project: " + ex.Message);
        }
    }
}
