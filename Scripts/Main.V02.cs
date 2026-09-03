using Godot;
using System;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    Label? _v02HardwareLabel;
    Label? _v02ImageStatus;
    Label? _v02ThreeDStatus;
    Button? _v02InstallImage;
    Button? _v02Install3D;
    Button? _v02RemoveImage;
    Button? _v02Remove3D;

    public void InstallV02Extras()
    {
        var aiPanel = FindChild("AI", true, false) as VBoxContainer;
        if (aiPanel == null) return;

        aiPanel.AddChild(new HSeparator());
        aiPanel.AddChild(new Label { Text = "AI Components — v0.2", ThemeTypeVariation = "HeaderSmall" });
        _v02HardwareLabel = new Label { Text = "Hardware: checking...", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        aiPanel.AddChild(_v02HardwareLabel);

        _v02ImageStatus = new Label { Text = "2D AI: checking...", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _v02ThreeDStatus = new Label { Text = "3D AI: checking...", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        aiPanel.AddChild(_v02ImageStatus);
        aiPanel.AddChild(_v02ThreeDStatus);

        var imageButtons = new HBoxContainer();
        _v02InstallImage = new Button { Text = "Install 2D AI", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v02RemoveImage = new Button { Text = "Remove", Disabled = true };
        _v02InstallImage.Pressed += async () => await InstallAiComponent("sd21", "2D AI");
        _v02RemoveImage.Pressed += async () => await RemoveAiComponent("sd21", "2D AI");
        imageButtons.AddChild(_v02InstallImage);
        imageButtons.AddChild(_v02RemoveImage);
        aiPanel.AddChild(imageButtons);

        var threeDButtons = new HBoxContainer();
        _v02Install3D = new Button { Text = "Install 3D AI", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v02Remove3D = new Button { Text = "Remove", Disabled = true };
        _v02Install3D.Pressed += async () => await InstallAiComponent("hunyuan21-shape", "3D AI");
        _v02Remove3D.Pressed += async () => await RemoveAiComponent("hunyuan21-shape", "3D AI");
        threeDButtons.AddChild(_v02Install3D);
        threeDButtons.AddChild(_v02Remove3D);
        aiPanel.AddChild(threeDButtons);

        var refresh = new Button { Text = "Refresh AI Component Status" };
        refresh.Pressed += async () => await RefreshAiComponents();
        aiPanel.AddChild(refresh);

        var release = new Button { Text = "Unload AI Models From VRAM" };
        release.Pressed += async () =>
        {
            await RunAi(async () =>
            {
                await _ai.ReleaseModelsAsync();
                SetStatus("AI models unloaded. VRAM will be reclaimed before the next generation.");
            });
        };
        aiPanel.AddChild(release);

        aiPanel.AddChild(new Label
        {
            Text = "v0.2 downloads model weights from the official provider repositories. On an 8 GB GPU, Hunyuan runs in low-VRAM/offload mode and may be slow.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        _ = RefreshAiComponents();
    }

    async Task RefreshAiComponents()
    {
        try
        {
            if (!await _ai.HealthAsync())
            {
                if (_v02HardwareLabel != null) _v02HardwareLabel.Text = "Hardware: AI backend is not running.";
                return;
            }
            var status = await _ai.GetComponentsAsync();
            string gpu = string.IsNullOrWhiteSpace(status.Hardware.Gpu) ? "No NVIDIA GPU detected" : status.Hardware.Gpu!;
            string vram = status.Hardware.VramMb > 0 ? $"{status.Hardware.VramMb / 1024.0:0.0} GB VRAM" : "VRAM unknown";
            if (_v02HardwareLabel != null)
                _v02HardwareLabel.Text = $"Hardware: {gpu} · {vram} · profile: {status.Hardware.RecommendedProfile}";

            bool imageInstalled = false, threeDInstalled = false;
            foreach (var component in status.Components)
            {
                if (component.Id == "sd21")
                {
                    imageInstalled = component.Installed;
                    if (_v02ImageStatus != null)
                        _v02ImageStatus.Text = $"2D AI: {component.Name} — {(component.Installed ? "installed" : $"not installed (~{component.EstimatedGb:0.#} GB)")}";
                }
                else if (component.Id == "hunyuan21-shape")
                {
                    threeDInstalled = component.Installed;
                    if (_v02ThreeDStatus != null)
                        _v02ThreeDStatus.Text = $"3D AI: {component.Name} — {(component.Installed ? "installed" : $"not installed (~{component.EstimatedGb:0.#} GB)")}";
                }
            }
            if (_v02InstallImage != null) _v02InstallImage.Disabled = imageInstalled;
            if (_v02RemoveImage != null) _v02RemoveImage.Disabled = !imageInstalled;
            if (_v02Install3D != null) _v02Install3D.Disabled = threeDInstalled;
            if (_v02Remove3D != null) _v02Remove3D.Disabled = !threeDInstalled;
        }
        catch (Exception ex)
        {
            SetStatus("AI component status failed: " + ex.Message);
        }
    }

    async Task InstallAiComponent(string id, string friendlyName)
    {
        await RunAi(async () =>
        {
            SetStatus($"Installing {friendlyName}. Large model downloads can take a while; do not close Miniscuplter.");
            SetAiComponentButtonsDisabled(true);
            try
            {
                await _ai.InstallComponentAsync(id);
                SetStatus($"{friendlyName} installed. Refreshing component status...");
            }
            finally
            {
                SetAiComponentButtonsDisabled(false);
            }
            await RefreshAiComponents();
        });
    }

    async Task RemoveAiComponent(string id, string friendlyName)
    {
        await RunAi(async () =>
        {
            await _ai.ReleaseModelsAsync();
            await _ai.UninstallComponentAsync(id);
            SetStatus($"{friendlyName} removed.");
            await RefreshAiComponents();
        });
    }

    void SetAiComponentButtonsDisabled(bool disabled)
    {
        if (_v02InstallImage != null) _v02InstallImage.Disabled = disabled;
        if (_v02Install3D != null) _v02Install3D.Disabled = disabled;
        if (_v02RemoveImage != null) _v02RemoveImage.Disabled = disabled;
        if (_v02Remove3D != null) _v02Remove3D.Disabled = disabled;
    }
}
