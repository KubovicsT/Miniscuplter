using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Miniscuplter;

public partial class Main
{
    sealed class V1023UiPreferences
    {
        public int BodySplitOffset { get; set; } = 235;
        public int WorkspaceSplitOffset { get; set; } = 890;
        public double FontScale { get; set; } = 0.9;
    }

    bool _v1023UiPreferencesInstalled;
    bool _v1023PreferenceSaveQueued;
    bool _v1023BodyReflowActive;
    bool _v1023WorkspaceReflowActive;
    V1023UiPreferences _v1023UiPreferences = new();
    VBoxContainer? _v1023Root;
    HSplitContainer? _v1023BodySplit;
    HSplitContainer? _v1023WorkspaceSplit;

    public void InstallV1023UiPreferences()
    {
        if (_v1023UiPreferencesInstalled) return;
        _v1023UiPreferencesInstalled = true;

        _v1023Root = GetChildren().OfType<VBoxContainer>().FirstOrDefault();
        _v1023BodySplit = _v1023Root?.GetChildren().OfType<HSplitContainer>().FirstOrDefault();
        _v1023WorkspaceSplit = FindChild("ViewportHost", true, false)?.GetParent() as HSplitContainer;

        V1023LoadPreferences();
        V1023ApplyFontScale(_v1023UiPreferences.FontScale, persist: false);
        CallDeferred(nameof(V1023ApplyPersistedLayout));

        if (_v1023BodySplit != null)
        {
            _v1023BodySplit.Dragged += _ => V1023QueuePreferenceSave();
            _v1023BodySplit.Resized += V1023ReflowBodySplit;
        }
        if (_v1023WorkspaceSplit != null)
            _v1023WorkspaceSplit.Dragged += _ => V1023QueuePreferenceSave();

        var settings = _v1011Toolbar?.GetChildren().OfType<Button>()
            .FirstOrDefault(button => button.Text.Equals("Settings", StringComparison.OrdinalIgnoreCase));
        if (settings != null)
            settings.Pressed += () => CallDeferred(nameof(V1023AttachInterfaceSettings));

        V1023InstallTooltips();
    }

    void V1023ApplyPersistedLayout()
    {
        V1023ReflowBodySplit();
        V1023ReflowWorkspaceSplit();
    }

    void V1023ReflowBodySplit()
    {
        if (!_v1023UiPreferencesInstalled || _v1023BodySplit == null || _v1023BodyReflowActive) return;
        _v1023BodyReflowActive = true;
        try
        {
            _v1023BodySplit.SplitOffset = V1023ClampSplit(
                _v1023UiPreferences.BodySplitOffset, _v1023BodySplit, 180, 260);
        }
        finally
        {
            _v1023BodyReflowActive = false;
        }
    }

    void V1023ReflowWorkspaceSplit()
    {
        if (!_v1023UiPreferencesInstalled || _v1023WorkspaceSplit == null || _v1023WorkspaceReflowActive) return;
        _v1023WorkspaceReflowActive = true;
        try
        {
            _v1023WorkspaceSplit.SplitOffset = V1023ClampSplit(
                _v1023UiPreferences.WorkspaceSplitOffset, _v1023WorkspaceSplit, 300, 260);
        }
        finally
        {
            _v1023WorkspaceReflowActive = false;
        }
    }

    static int V1023ClampSplit(
        int requested,
        SplitContainer split,
        int preferredMinimumPrimary,
        int preferredMinimumSecondary)
    {
        int width = Math.Max(1, (int)Math.Floor(split.Size.X));
        int separator = Math.Clamp(split.GetThemeConstant("separation"), 0, Math.Max(0, width - 1));
        int available = Math.Max(1, width - separator);

        // Prefer both panel minimums when they fit. On an extremely narrow client, reduce the
        // floors before Math.Clamp so lower can never exceed upper and the primary viewport
        // receives the remaining usable span instead of forcing the root beyond the client rect.
        int minimumPrimary = Math.Min(Math.Max(1, preferredMinimumPrimary), available);
        int minimumSecondary = Math.Min(
            Math.Max(0, preferredMinimumSecondary),
            Math.Max(0, available - minimumPrimary));
        int maximumPrimary = Math.Max(minimumPrimary, available - minimumSecondary);
        return Math.Clamp(requested, minimumPrimary, maximumPrimary);
    }

    void V1023ApplyFontScale(double requested, bool persist)
    {
        double scale = Math.Clamp(requested, 0.75, 1.35);
        _v1023UiPreferences.FontScale = scale;
        if (_v1023Root == null) return;

        var theme = _v1023Root.Theme?.Duplicate() as Theme ?? new Theme();
        theme.DefaultFontSize = Math.Clamp((int)Math.Round(16 * scale), 12, 22);
        _v1023Root.Theme = theme;

        if (persist)
            V1023QueuePreferenceSave();
    }

    void V1023AttachInterfaceSettings()
    {
        if (_v109SettingsWindow == null || !GodotObject.IsInstanceValid(_v109SettingsWindow)) return;
        if (_v109SettingsWindow.FindChild("TabContainer", true, false) is not TabContainer tabs) return;
        if (tabs.GetChildren().OfType<Control>().Any(control => control.Name.ToString() == "Interface")) return;

        var page = new VBoxContainer
        {
            Name = "Interface",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        page.AddChild(new Label { Text = "INTERFACE", ThemeTypeVariation = "HeaderSmall" });
        page.AddChild(new Label
        {
            Text = "Workspace panel widths and interface text size are editor preferences. They are stored separately from project/model state.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        var row = new HBoxContainer();
        row.AddChild(new Label { Text = "UI / font scale", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        var scale = new SpinBox
        {
            MinValue = 0.75,
            MaxValue = 1.35,
            Step = 0.05,
            Value = _v1023UiPreferences.FontScale,
            CustomMinimumSize = new Vector2(120, 0),
            TooltipText = "Scale interface text between 75% and 135%. The setting is restored on the next launch."
        };
        scale.ValueChanged += value => V1023ApplyFontScale(value, persist: true);
        row.AddChild(scale);
        page.AddChild(row);

        var reset = new Button
        {
            Text = "Reset Workspace Layout",
            TooltipText = "Restore default panel widths while keeping project/model state unchanged."
        };
        reset.Pressed += () =>
        {
            _v1023UiPreferences.BodySplitOffset = 235;
            _v1023UiPreferences.WorkspaceSplitOffset = 890;
            V1023ApplyPersistedLayout();
            V1023QueuePreferenceSave();
        };
        page.AddChild(reset);

        tabs.AddChild(page);
        tabs.SetTabTitle(tabs.GetTabCount() - 1, "Interface");
    }

    void V1023InstallTooltips()
    {
        V1023SetTooltip("New", "Start a clean project. Project data is not silently overwritten.");
        V1023SetTooltip("Import STL", "Import an STL as a new scene object.");
        V1023SetTooltip("Export STL", "Export the currently selected/validated model scope to STL.");
        V1023SetTooltip("Undo", "Undo the latest supported editor/project transaction.");
        V1023SetTooltip("Redo", "Redo the latest supported editor/project transaction.");
        V1023SetTooltip("Frame", "Frame the selected object in the 3D viewport.");
        V1023SetTooltip("Settings", "Open AI, storage, viewport and interface preferences.");
    }

    void V1023SetTooltip(string buttonText, string tooltip)
    {
        foreach (Button button in FindChildren("*", "Button", true, false).OfType<Button>()
                     .Where(button => button.Text.Equals(buttonText, StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrWhiteSpace(button.TooltipText))
                button.TooltipText = tooltip;
        }
    }

    void V1023QueuePreferenceSave()
    {
        if (_v1023BodySplit != null)
            _v1023UiPreferences.BodySplitOffset = _v1023BodySplit.SplitOffset;
        if (_v1023WorkspaceSplit != null)
            _v1023UiPreferences.WorkspaceSplitOffset = _v1023WorkspaceSplit.SplitOffset;

        if (_v1023PreferenceSaveQueued) return;
        _v1023PreferenceSaveQueued = true;
        CallDeferred(nameof(V1023SavePreferences));
    }

    void V1023LoadPreferences()
    {
        string path = AppDataRoot.Resolve("Settings/ui_preferences.json");
        if (!File.Exists(path)) return;
        try
        {
            var loaded = JsonSerializer.Deserialize<V1023UiPreferences>(File.ReadAllText(path));
            if (loaded != null)
                _v1023UiPreferences = loaded;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Ignoring invalid UI preferences: {ex.Message}");
            _v1023UiPreferences = new V1023UiPreferences();
        }
    }

    void V1023SavePreferences()
    {
        _v1023PreferenceSaveQueued = false;
        try
        {
            string path = AppDataRoot.Resolve("Settings/ui_preferences.json");
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(_v1023UiPreferences, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, path, true);
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Could not save UI preferences: {ex.Message}");
        }
    }
}
