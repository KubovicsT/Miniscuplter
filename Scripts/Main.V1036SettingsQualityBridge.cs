using Godot;
using System;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    bool _v1036SettingsQualityHooked;

    public void InstallV1036SettingsQualityBridge()
    {
        if (_v1036SettingsQualityHooked) return;
        Button? settings = GetTree().Root.FindChildren("*", "Button", true, false)
            .OfType<Button>()
            .FirstOrDefault(button => button.Text.Equals("Settings", StringComparison.Ordinal));
        if (settings == null) return;

        _v1036SettingsQualityHooked = true;
        settings.Pressed += () => CallDeferred(nameof(V1036ExposeQualitySettings));
    }

    void V1036ExposeQualitySettings()
    {
        Window? window = GetTree().Root.FindChildren("*", "Window", true, false)
            .OfType<Window>()
            .FirstOrDefault(candidate => candidate.Title.Equals("Settings", StringComparison.Ordinal));
        if (window == null) return;
        TabContainer? tabs = window.FindChildren("*", "TabContainer", true, false).OfType<TabContainer>().FirstOrDefault();
        if (tabs == null) return;

        VBoxContainer? quality = tabs.GetChildren().OfType<VBoxContainer>()
            .FirstOrDefault(page => page.Name.ToString().Equals("Quality", StringComparison.OrdinalIgnoreCase));
        if (quality == null || quality.FindChild("QualityParameterSummaryV1036", false, false) != null) return;

        quality.AddChild(new HSeparator { Name = "QualityParameterSummaryV1036" });
        quality.AddChild(new Label { Text = "ACTIVE PRESET PARAMETERS", ThemeTypeVariation = "HeaderSmall" });
        V097QualityPreset? preset = _v097ActivePreset;
        if (preset != null)
        {
            var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            V1036AddQualityValue(grid, "2D resolution", $"{preset.ImageSize}px");
            V1036AddQualityValue(grid, "2D inference steps", preset.ImageSteps.ToString());
            V1036AddQualityValue(grid, "2D guidance", preset.ImageGuidance.ToString("0.0"));
            V1036AddQualityValue(grid, "3D inference steps", preset.ShapeSteps.ToString());
            V1036AddQualityValue(grid, "Remesh voxel", $"{preset.RemeshVoxelMm:0.00} mm");
            V1036AddQualityValue(grid, "Repair voxel", $"{preset.RepairVoxelMm:0.00} mm");
            V1036AddQualityValue(grid, "Voxel budget", $"{preset.MaxVoxelCells:N0}");
            V1036AddQualityValue(grid, "Smart Select", $"{preset.SmartSelectViews} views · {preset.SmartSelectRenderSize}px");
            quality.AddChild(grid);
        }

        var edit = new Button
        {
            Text = "Edit Parameters / Create Custom Preset…",
            TooltipText = "Open the full quality editor. Built-in presets stay read-only; save edited values as a custom preset."
        };
        edit.Pressed += () => V1036OpenQualityPresetEditor(window);
        quality.AddChild(edit);
    }

    static void V1036AddQualityValue(GridContainer grid, string name, string value)
    {
        grid.AddChild(new Label { Text = name });
        grid.AddChild(new Label { Text = value, HorizontalAlignment = HorizontalAlignment.Right });
    }

    void V1036OpenQualityPresetEditor(Window settingsWindow)
    {
        TabContainer? mainTabs = FindChild("TabContainer", true, false) as TabContainer;
        if (mainTabs == null) return;
        for (int i = 0; i < mainTabs.GetTabCount(); i++)
        {
            Control page = mainTabs.GetChild<Control>(i);
            if (!page.Name.ToString().Equals("Quality v0.9.7", StringComparison.OrdinalIgnoreCase)) continue;
            mainTabs.CurrentTab = i;
            settingsWindow.Hide();
            SetStatus("Quality preset editor opened — edit parameters and save a custom preset when needed.");
            return;
        }
    }
}
