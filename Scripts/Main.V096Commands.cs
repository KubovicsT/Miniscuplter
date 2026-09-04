using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    PopupPanel? _v096CommandPopup;
    LineEdit? _v096CommandInput;
    VBoxContainer? _v096Suggestions;
    readonly List<string> _v096CommandHistory = new();
    int _v096HistoryIndex = -1;

    static readonly string[] V096Commands =
    {
        "/s ", "/s+ ", "/s- ", "/clear", "/invert", "/grow", "/shrink", "/smooth",
        "/hide", "/hide selection", "/show", "/isolate", "/isolate selection", "/frame", "/duplicate", "/delete",
        "/remesh ", "/analyze", "/thickness ", "/rig quick", "/rig universal",
        "/pose preview", "/pose reset", "/pose apply", "/savepart", "/edit ",
        "/detail2d ", "/detail3d ", "/detail apply", "/detail discard", "/ai routes", "/help"
    };

    public void InstallV096CommandPalette()
    {
        if (_v096CommandPopup != null) return;
        var popup = new PopupPanel { Name = "Command Palette", Size = new Vector2I(430, 220) };
        var box = new VBoxContainer();
        box.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        box.AddThemeConstantOverride("separation", 4);
        var input = new LineEdit { PlaceholderText = "/s head", ClearButtonEnabled = true };
        var suggestions = new VBoxContainer();
        box.AddChild(input); box.AddChild(suggestions); popup.AddChild(box); AddChild(popup);
        _v096CommandPopup = popup; _v096CommandInput = input; _v096Suggestions = suggestions;
        input.TextChanged += _ => RefreshV096Suggestions();
        input.TextSubmitted += async text => await ExecuteV096CommandAsync(text);
        input.GuiInput += OnV096CommandInput;
        popup.PopupHide += () => { _v096HistoryIndex = -1; };
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == Godot.Key.Space && !V096TextInputHasFocus())
        {
            OpenV096CommandPalette(); GetViewport().SetInputAsHandled(); return;
        }
        if (key.Keycode == Godot.Key.Escape && _v096CommandPopup?.Visible == true)
        {
            _v096CommandPopup.Hide(); GetViewport().SetInputAsHandled();
        }
    }

    bool V096TextInputHasFocus()
    {
        var focus = GetViewport().GuiGetFocusOwner();
        if (focus == null || focus == _v096CommandInput) return false;
        return focus is LineEdit or TextEdit or SpinBox;
    }

    void OpenV096CommandPalette()
    {
        if (_v096CommandPopup == null || _v096CommandInput == null) return;
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector2 vpSize = GetViewport().GetVisibleRect().Size;
        int x = Math.Clamp((int)mouse.X + 12, 0, Math.Max(0, (int)vpSize.X - _v096CommandPopup.Size.X));
        int y = Math.Clamp((int)mouse.Y + 12, 0, Math.Max(0, (int)vpSize.Y - _v096CommandPopup.Size.Y));
        _v096CommandPopup.Position = new Vector2I(x, y);
        _v096CommandInput.Text = "/"; _v096CommandInput.CaretColumn = 1;
        RefreshV096Suggestions(); _v096CommandPopup.Popup(); _v096CommandInput.GrabFocus();
    }

    void OnV096CommandInput(InputEvent ev)
    {
        if (ev is not InputEventKey key || !key.Pressed || key.Echo || _v096CommandInput == null) return;
        if (key.Keycode == Godot.Key.Up && _v096CommandHistory.Count > 0)
        {
            _v096HistoryIndex = Math.Clamp(_v096HistoryIndex < 0 ? _v096CommandHistory.Count - 1 : _v096HistoryIndex - 1, 0, _v096CommandHistory.Count - 1);
            _v096CommandInput.Text = _v096CommandHistory[_v096HistoryIndex]; _v096CommandInput.CaretColumn = _v096CommandInput.Text.Length; GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Godot.Key.Down && _v096CommandHistory.Count > 0)
        {
            _v096HistoryIndex = Math.Min(_v096CommandHistory.Count - 1, Math.Max(0, _v096HistoryIndex + 1));
            _v096CommandInput.Text = _v096CommandHistory[_v096HistoryIndex]; _v096CommandInput.CaretColumn = _v096CommandInput.Text.Length; GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Godot.Key.Escape) { _v096CommandPopup?.Hide(); GetViewport().SetInputAsHandled(); }
    }

    void RefreshV096Suggestions()
    {
        if (_v096Suggestions == null || _v096CommandInput == null) return;
        foreach (var c in _v096Suggestions.GetChildren()) c.QueueFree();
        string q = _v096CommandInput.Text.Trim();
        foreach (string cmd in V096Commands.Where(c => c.StartsWith(q, StringComparison.OrdinalIgnoreCase) || q == "/").Take(7))
        {
            var b = new Button { Text = cmd.TrimEnd(), Alignment = HorizontalAlignment.Left };
            b.Pressed += () => { if (_v096CommandInput == null) return; _v096CommandInput.Text = cmd; _v096CommandInput.CaretColumn = cmd.Length; _v096CommandInput.GrabFocus(); };
            _v096Suggestions.AddChild(b);
        }
    }

    async Task ExecuteV096CommandAsync(string raw)
    {
        string text = (raw ?? "").Trim(); if (text.Length == 0) return;
        string? last = _v096CommandHistory.LastOrDefault();
        if (last == null || !last.Equals(text, StringComparison.OrdinalIgnoreCase)) _v096CommandHistory.Add(text);
        while (_v096CommandHistory.Count > 50) _v096CommandHistory.RemoveAt(0);
        _v096CommandPopup?.Hide();
        try
        {
            if (text.StartsWith("/s+", StringComparison.OrdinalIgnoreCase)) { await SmartSelectV096Async(text[3..].Trim(), '+'); return; }
            if (text.StartsWith("/s-", StringComparison.OrdinalIgnoreCase)) { await SmartSelectV096Async(text[3..].Trim(), '-'); return; }
            if (text.StartsWith("/s ", StringComparison.OrdinalIgnoreCase)) { await SmartSelectV096Async(text[3..].Trim(), '='); return; }
            string[] parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries); string cmd = parts[0].ToLowerInvariant(); string arg = parts.Length > 1 ? parts[1].Trim() : "";
            switch (cmd)
            {
                case "/clear": ClearV096Selection(); break;
                case "/invert": InvertV096Selection(); break;
                case "/grow": RefineV096Selection("grow", V096ParseRefineIterations(arg)); break;
                case "/shrink": RefineV096Selection("shrink", V096ParseRefineIterations(arg)); break;
                case "/smooth": RefineV096Selection("smooth", V096ParseRefineIterations(arg)); break;
                case "/hide": V096HideCommand(arg); break;
                case "/show": V096RestoreSelectionView(); foreach (var o in _objects.Where(GodotObject.IsInstanceValid)) o.Visible = true; SetStatus("All scene objects shown."); break;
                case "/isolate": V096IsolateCommand(arg); break;
                case "/frame": FrameSelected(); break;
                case "/duplicate": DuplicateSelected(); break;
                case "/delete": DeleteSelected(); break;
                case "/remesh": await V096CommandRemesh(arg); break;
                case "/analyze": await AnalyzeSelectedV09Async(); break;
                case "/thickness": await V096CommandThickness(arg); break;
                case "/rig": await GenerateV06Rig(arg.Equals("universal", StringComparison.OrdinalIgnoreCase) ? "universal" : "quick"); break;
                case "/pose": V096PoseCommand(arg); break;
                case "/savepart": V096SavePartCommand(arg); break;
                case "/edit": await V096EditCommand(arg); break;
                case "/detail2d": await V098Detail2DAsync(arg); break;
                case "/detail3d": await V098Detail3DAsync(arg); break;
                case "/detail":
                    if (arg.Equals("apply", StringComparison.OrdinalIgnoreCase)) await V098ApplyDetailAsync();
                    else if (arg.Equals("discard", StringComparison.OrdinalIgnoreCase)) V098DiscardDetail();
                    else await V098Detail3DAsync(arg);
                    break;
                case "/ai":
                    if (arg.Equals("routes", StringComparison.OrdinalIgnoreCase)) { await RefreshV098RoutingOnly(); SetStatus(_v098Routing?.Text ?? "AI routing refreshed."); }
                    else SetStatus("Use /ai routes to inspect automatic model routing.");
                    break;
                case "/help": SetStatus("Commands: /s, /s+, /s-, /clear, /invert, /grow [1-10], /shrink [1-10], /smooth [1-10], /hide [selection], /show, /isolate [selection], /frame, /duplicate, /delete, /remesh [0.04-5 mm], /analyze, /thickness [mm], /rig quick|universal, /pose preview|reset|apply, /savepart [name], /edit <prompt>, /detail2d <prompt>, /detail3d <prompt>, /detail apply|discard, /ai routes."); break;
                default: SetStatus($"Unknown command '{cmd}'. Type /help."); break;
            }
        }
        catch (Exception ex) { SetStatus("Command failed safely: " + ex.Message); }
    }

    int V096ParseRefineIterations(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg)) return 1;
        if (!int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || n < 1 || n > 10)
            throw new ArgumentOutOfRangeException(nameof(arg), "Selection refinement passes must be 1–10.");
        return n;
    }

    void V096HideCommand(string arg)
    {
        if (arg.Equals("selection", StringComparison.OrdinalIgnoreCase)) { V096SetSelectionView(showSelected: false); return; }
        if (_selected != null) { _selected.Visible = false; SetStatus($"Hidden {_selected.Name}."); }
    }

    void V096IsolateCommand(string arg)
    {
        if (arg.Equals("selection", StringComparison.OrdinalIgnoreCase)) { V096SetSelectionView(showSelected: true); return; }
        if (_selected != null) { V096RestoreSelectionView(); foreach (var o in _objects.Where(GodotObject.IsInstanceValid)) o.Visible = o == _selected; SetStatus($"Isolated {_selected.Name}."); }
    }

    async Task V096CommandRemesh(string arg)
    {
        if (!string.IsNullOrWhiteSpace(arg))
        {
            if (!double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out double pitch))
                throw new ArgumentException("Use /remesh by itself for the active preset pitch, or /remesh <0.04-5 mm>.", nameof(arg));
            if (pitch < .04 || pitch > 5.0) throw new ArgumentOutOfRangeException(nameof(arg), "Remesh pitch must be 0.04–5.0 mm.");
            if (_v08RemeshVoxel != null) _v08RemeshVoxel.Value = pitch;
        }
        await V08RemeshSelectedAsync();
    }

    async Task V096CommandThickness(string arg)
    {
        if (double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out double target))
        {
            if (target <= 0 || target > 100) throw new ArgumentOutOfRangeException(nameof(arg), "Thickness target must be >0 and <=100 mm.");
            if (_v09ThicknessTarget != null) _v09ThicknessTarget.Value = target;
        }
        await AnalyzeThicknessV09Async();
    }

    void V096PoseCommand(string arg)
    {
        switch (arg.ToLowerInvariant())
        {
            case "preview": ApplyV06PosePreview(); SetStatus("Pose preview refreshed."); break;
            case "reset": ResetV06Pose(); break;
            case "apply": CommitV06Pose(); break;
            default: SetStatus("Use /pose preview, /pose reset, or /pose apply."); break;
        }
    }

    void V096SavePartCommand(string arg)
    {
        if (_selected == null) { SetStatus("Select an object first."); return; }
        string old = _selected.Name.ToString();
        try { if (!string.IsNullOrWhiteSpace(arg)) _selected.Name = arg.Trim(); SafeV095SaveSelectedAsPart(); }
        finally { if (!string.IsNullOrWhiteSpace(arg) && GodotObject.IsInstanceValid(_selected)) _selected.Name = old; }
    }

    async Task V096EditCommand(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) { SetStatus("Use /edit followed by the requested change."); return; }
        V096ValidateSelection();
        if (_v096Selection == null) { SetStatus("Create a Smart Selection first, for example /s head."); return; }
        string mask = BuildV096ViewportMask(); CaptureView();
        string output = ProjectSettings.GlobalizePath($"user://smart_edit_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        await RunAi(async () =>
        {
            _lastEditedImage = await _ai.EditImageAsync(_lastCapture, mask, prompt, output, CurrentQuality());
            ShowAiPreview(_lastEditedImage); SetStatus("AI edit generated from the active Smart Selection. Review it or generate a 3D patch from the approved result.");
        });
    }
}
