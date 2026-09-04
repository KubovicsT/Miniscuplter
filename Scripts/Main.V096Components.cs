using Godot;
using System;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    Label? _v096SemanticStatus;
    Button? _v096InstallSemantic;
    Button? _v096RemoveSemantic;

    public void InstallV096SemanticComponent()
    {
        var aiPanel = FindChild("AI", true, false) as VBoxContainer;
        if (aiPanel == null) return;
        aiPanel.AddChild(new HSeparator());
        aiPanel.AddChild(new Label { Text = "SMART SELECT AI — v0.9.6", ThemeTypeVariation = "HeaderSmall" });
        _v096SemanticStatus = new Label { Text = "Semantic AI: checking...", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        aiPanel.AddChild(_v096SemanticStatus);
        var row = new HBoxContainer();
        _v096InstallSemantic = new Button { Text = "Install Smart Select AI", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v096RemoveSemantic = new Button { Text = "Remove", Disabled = true };
        _v096InstallSemantic.Pressed += async () => await V096InstallSemanticAsync();
        _v096RemoveSemantic.Pressed += async () => await V096RemoveSemanticAsync();
        row.AddChild(_v096InstallSemantic); row.AddChild(_v096RemoveSemantic); aiPanel.AddChild(row);
        aiPanel.AddChild(new Label
        {
            Text = "CLIPSeg performs local text-guided semantic segmentation on six rendered views of the selected mesh. Metadata and rig-aware selection remain available even without it.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        _ = V096RefreshSemanticStatus();
    }

    async Task V096RefreshSemanticStatus()
    {
        try
        {
            if (!await _ai.HealthAsync())
            {
                if (_v096SemanticStatus != null) _v096SemanticStatus.Text = "Semantic AI: backend is not running.";
                return;
            }
            var status = await _ai.GetComponentsAsync();
            var item = status.Components.Find(c => c.Id == "clipseg-smart-select");
            bool installed = item?.Installed == true;
            if (_v096SemanticStatus != null)
                _v096SemanticStatus.Text = item == null ? "Semantic AI: component unavailable in backend." : $"Semantic AI: {item.Name} — {(installed ? "installed" : $"not installed (~{item.EstimatedGb:0.#} GB)")}";
            if (_v096InstallSemantic != null) _v096InstallSemantic.Disabled = installed;
            if (_v096RemoveSemantic != null) _v096RemoveSemantic.Disabled = !installed;
        }
        catch (Exception ex) { SetStatus("Smart Select AI status failed: " + ex.Message); }
    }

    async Task V096InstallSemanticAsync()
    {
        await RunAi(async () =>
        {
            if (_v096InstallSemantic != null) _v096InstallSemantic.Disabled = true;
            if (_v096RemoveSemantic != null) _v096RemoveSemantic.Disabled = true;
            try
            {
                SetStatus("Installing local CLIPSeg Smart Select AI...");
                await _ai.InstallComponentAsync("clipseg-smart-select");
                SetStatus("Smart Select AI installed.");
            }
            finally { await V096RefreshSemanticStatus(); }
        });
    }

    async Task V096RemoveSemanticAsync()
    {
        await RunAi(async () =>
        {
            await _ai.ReleaseModelsAsync();
            await _ai.UninstallComponentAsync("clipseg-smart-select");
            SetStatus("Smart Select AI removed; metadata/rig/geometry fallback remains available.");
            await V096RefreshSemanticStatus();
        });
    }
}
