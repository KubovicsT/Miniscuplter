using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    enum AiConsoleAction
    {
        GenerateConcept,
        EditSelectedRegion,
        SmartSelect,
        Generate3DFromAcceptedImage,
        Detail3DPreview
    }

    TextEdit? _aiCommandInput;
    VBoxContainer? _aiCommandHistoryList;
    ScrollContainer? _aiCommandHistoryScroll;
    readonly List<string> _aiCommandHistory = new();
    int _aiCommandHistoryIndex = -1;

    public void InstallAiCommandConsole()
    {
        if (_aiCommandInput != null) return;
        var toolbar = _v1011Toolbar;
        if (toolbar?.GetParent() is not VBoxContainer root) return;

        var panel = new VBoxContainer
        {
            Name = "AI Command Console",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        panel.AddThemeConstantOverride("separation", 3);

        var commandRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var input = new TextEdit
        {
            Name = "AI Command Input",
            PlaceholderText = "AI command or prompt…",
            CustomMinimumSize = new Vector2(0, 72),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ScrollFitContentHeight = false,
            WrapMode = TextEdit.LineWrappingMode.Boundary,
            TooltipText = "Multi-line AI command console. Use Run (or Ctrl+Enter) to execute; Ctrl+Up/Down navigates command history."
        };
        var controls = new VBoxContainer { CustomMinimumSize = new Vector2(74, 0) };
        var historyRow = new HBoxContainer();
        var previous = new Button { Text = "◀", TooltipText = "Previous AI command", CustomMinimumSize = new Vector2(34, 28) };
        var next = new Button { Text = "▶", TooltipText = "Next AI command", CustomMinimumSize = new Vector2(34, 28) };
        var run = new Button { Text = "Run", TooltipText = "Run through the authoritative AI command dispatcher", CustomMinimumSize = new Vector2(72, 36) };
        historyRow.AddChild(previous);
        historyRow.AddChild(next);
        controls.AddChild(historyRow);
        controls.AddChild(run);
        commandRow.AddChild(input);
        commandRow.AddChild(controls);
        panel.AddChild(commandRow);

        var actionRow = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin };
        AddAiConsoleButton(actionRow, "2D Concept", AiConsoleAction.GenerateConcept,
            "Generate a 2D concept from the command text using the existing local concept-generation path.");
        AddAiConsoleButton(actionRow, "Edit Region", AiConsoleAction.EditSelectedRegion,
            "Edit the active Smart Selection using the existing revision-safe 2D edit path.");
        AddAiConsoleButton(actionRow, "Select", AiConsoleAction.SmartSelect,
            "Create the existing semantic Smart Selection from the command text.");
        AddAiConsoleButton(actionRow, "Generate 3D", AiConsoleAction.Generate3DFromAcceptedImage,
            "Generate 3D from the durable accepted baseline through the Stage-C candidate path.");
        AddAiConsoleButton(actionRow, "3D Detail", AiConsoleAction.Detail3DPreview,
            "Generate the existing non-destructive selected-detail 3D preview.");
        panel.AddChild(actionRow);

        _aiCommandHistoryScroll = new ScrollContainer
        {
            Name = "AI Command History",
            CustomMinimumSize = new Vector2(0, 52),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            TooltipText = "Recent AI commands. Use Ctrl+Up/Down or the arrow buttons to reuse them."
        };
        _aiCommandHistoryList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _aiCommandHistoryScroll.AddChild(_aiCommandHistoryList);
        panel.AddChild(_aiCommandHistoryScroll);

        root.AddChild(panel);
        root.MoveChild(panel, Math.Min(toolbar.GetIndex() + 1, root.GetChildCount() - 1));
        _aiCommandInput = input;

        input.GuiInput += OnAiCommandInput;
        run.Pressed += async () => await ExecuteAiConsoleInputAsync(input.Text);
        previous.Pressed += () => NavigateAiCommandHistory(-1);
        next.Pressed += () => NavigateAiCommandHistory(1);
    }

    void AddAiConsoleButton(Container parent, string text, AiConsoleAction action, string tooltip)
    {
        var button = new Button
        {
            Text = text,
            TooltipText = tooltip,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin
        };
        button.Pressed += async () => await DispatchAiConsoleActionAsync(action, _aiCommandInput?.Text ?? "");
        parent.AddChild(button);
    }

    void OnAiCommandInput(InputEvent ev)
    {
        if (ev is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.CtrlPressed && key.Keycode == Godot.Key.Enter)
        {
            _ = ExecuteAiConsoleInputAsync(_aiCommandInput?.Text ?? "");
            GetViewport().SetInputAsHandled();
        }
        else if (key.CtrlPressed && key.Keycode == Godot.Key.Up)
        {
            NavigateAiCommandHistory(-1);
            GetViewport().SetInputAsHandled();
        }
        else if (key.CtrlPressed && key.Keycode == Godot.Key.Down)
        {
            NavigateAiCommandHistory(1);
            GetViewport().SetInputAsHandled();
        }
    }

    void NavigateAiCommandHistory(int direction)
    {
        if (_aiCommandInput == null || _aiCommandHistory.Count == 0) return;
        if (_aiCommandHistoryIndex < 0)
            _aiCommandHistoryIndex = direction < 0 ? _aiCommandHistory.Count - 1 : 0;
        else
            _aiCommandHistoryIndex = Math.Clamp(_aiCommandHistoryIndex + direction, 0, _aiCommandHistory.Count - 1);
        _aiCommandInput.Text = _aiCommandHistory[_aiCommandHistoryIndex];
        _aiCommandInput.GrabFocus();
        RefreshAiCommandHistoryLabel();
    }

    void RecordAiCommand(string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return;
        if (_aiCommandHistory.Count == 0 || !_aiCommandHistory[^1].Equals(text, StringComparison.Ordinal))
            _aiCommandHistory.Add(text);
        while (_aiCommandHistory.Count > 50) _aiCommandHistory.RemoveAt(0);
        _aiCommandHistoryIndex = _aiCommandHistory.Count - 1;
        RefreshAiCommandHistoryLabel();
    }

    void RefreshAiCommandHistoryLabel()
    {
        if (_aiCommandHistoryList == null) return;
        foreach (Node child in _aiCommandHistoryList.GetChildren())
            child.QueueFree();

        int start = Math.Max(0, _aiCommandHistory.Count - 8);
        for (int i = start; i < _aiCommandHistory.Count; i++)
        {
            var label = new Label
            {
                Text = $"{i + 1}. {_aiCommandHistory[i]}",
                TooltipText = _aiCommandHistory[i],
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            _aiCommandHistoryList.AddChild(label);
        }

        if (_aiCommandHistoryScroll != null)
            CallDeferred(nameof(ScrollAiCommandHistoryToBottom));
    }

    void ScrollAiCommandHistoryToBottom()
    {
        if (_aiCommandHistoryScroll == null) return;
        _aiCommandHistoryScroll.ScrollVertical = int.MaxValue;
    }

    async Task ExecuteAiConsoleInputAsync(string raw)
    {
        string text = (raw ?? "").Trim();
        if (text.Length == 0)
        {
            SetStatus("Enter an AI prompt or command first.");
            return;
        }
        RecordAiCommand(text);

        if (text.StartsWith("/", StringComparison.Ordinal))
        {
            await ExecuteV096CommandAsync(text);
            return;
        }

        if (TryAiConsolePrefix(text, "concept", out string concept))
        {
            await DispatchAiConsoleActionAsync(AiConsoleAction.GenerateConcept, concept);
            return;
        }
        if (TryAiConsolePrefix(text, "edit", out string edit))
        {
            await DispatchAiConsoleActionAsync(AiConsoleAction.EditSelectedRegion, edit);
            return;
        }
        if (TryAiConsolePrefix(text, "select", out string select))
        {
            await DispatchAiConsoleActionAsync(AiConsoleAction.SmartSelect, select);
            return;
        }
        if (TryAiConsolePrefix(text, "3d", out string threeD))
        {
            await DispatchAiConsoleActionAsync(AiConsoleAction.Generate3DFromAcceptedImage, threeD);
            return;
        }
        if (TryAiConsolePrefix(text, "detail3d", out string detail))
        {
            await DispatchAiConsoleActionAsync(AiConsoleAction.Detail3DPreview, detail);
            return;
        }

        await DispatchAiConsoleActionAsync(AiConsoleAction.GenerateConcept, text);
    }

    static bool TryAiConsolePrefix(string text, string prefix, out string argument)
    {
        argument = "";
        if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase)) return true;
        if (!text.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase)) return false;
        argument = text[(prefix.Length + 1)..].Trim();
        return true;
    }

    async Task DispatchAiConsoleActionAsync(AiConsoleAction action, string argument)
    {
        string prompt = (argument ?? "").Trim();
        if (prompt.Length > 0 && _prompt != null) _prompt.Text = prompt;

        switch (action)
        {
            case AiConsoleAction.GenerateConcept:
                await GenerateConcept();
                break;
            case AiConsoleAction.EditSelectedRegion:
                await V096EditCommand(prompt);
                break;
            case AiConsoleAction.SmartSelect:
                await SmartSelectV096Async(prompt);
                break;
            case AiConsoleAction.Generate3DFromAcceptedImage:
                V1020Generate3DAsync();
                break;
            case AiConsoleAction.Detail3DPreview:
                await V098Detail3DAsync(prompt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }
}
