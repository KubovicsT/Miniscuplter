using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    VBoxContainer? _v108AiJobPanel;
    Label? _v108AiJobStatus;
    Label? _v108AiJobDetail;
    ProgressBar? _v108AiActivity;
    Button? _v108GenerateConcept;
    Button? _v108CancelAi;
    Timer? _v108AiTimer;
    DateTime _v108AiStarted;
    bool _v108AiBusy;
    string _v108AiProvider = "auto";

    public void InstallV108AiFeedback()
    {
        var aiPanel = FindChild("AI", true, false) as VBoxContainer;
        if (aiPanel == null) return;

        int insertAt = 3;
        foreach (var child in aiPanel.GetChildren())
        {
            if (child is Button b && b.Text == "Generate Concept")
            {
                insertAt = b.GetIndex();
                b.Disabled = true;
                b.Visible = false;
                break;
            }
        }

        _v108AiJobPanel = new VBoxContainer { Name = "AI Job Feedback v1.0.8" };
        _v108GenerateConcept = new Button { Text = "Generate Concept", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v108GenerateConcept.Pressed += V108GenerateConceptAsync;
        _v108AiJobPanel.AddChild(_v108GenerateConcept);

        _v108AiJobStatus = new Label
        {
            Text = "AI status: idle",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _v108AiJobPanel.AddChild(_v108AiJobStatus);

        _v108AiActivity = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            ShowPercentage = false,
            Visible = false,
            CustomMinimumSize = new Vector2(0, 12)
        };
        _v108AiJobPanel.AddChild(_v108AiActivity);

        _v108AiJobDetail = new Label
        {
            Text = "",
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _v108AiJobPanel.AddChild(_v108AiJobDetail);

        _v108CancelAi = new Button { Text = "Cancel AI Job", Visible = false };
        _v108CancelAi.Pressed += () =>
        {
            if (!_v108AiBusy) return;
            _ai.CancelCurrentRequest();
            if (_v108AiJobStatus != null) _v108AiJobStatus.Text = "AI status: cancelling…";
        };
        _v108AiJobPanel.AddChild(_v108CancelAi);

        aiPanel.AddChild(_v108AiJobPanel);
        aiPanel.MoveChild(_v108AiJobPanel, Math.Clamp(insertAt, 0, aiPanel.GetChildCount() - 1));

        _v108AiTimer = new Timer { WaitTime = 0.75, OneShot = false };
        _v108AiTimer.Timeout += V108TickAiActivity;
        AddChild(_v108AiTimer);
    }

    async void V108GenerateConceptAsync()
    {
        if (_v108AiBusy) return;
        string prompt = _prompt?.Text.Trim() ?? "";
        if (prompt.Length == 0)
        {
            V108SetAiResult("AI status: enter a prompt first.", "Nothing was sent to the backend.");
            return;
        }

        string outPath = ProjectSettings.GlobalizePath($"user://concept_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        _v108AiBusy = true;
        _v108AiStarted = DateTime.UtcNow;
        _v108AiProvider = "auto";
        V108SetAiBusy(true);

        try
        {
            V108SetAiPhase("Checking local AI service…", "The editor is verifying that the bundled backend responds before starting generation.");
            if (!await _ai.HealthAsync())
                throw new InvalidOperationException("The local AI backend did not answer its health check. Use Repair AI Runtime in the launcher and restart the editor.");

            if (_v097ActivePreset != null)
            {
                V108SetAiPhase("Applying quality preset…", $"Preset: {_v097ActivePreset.Name} · {_v097ActivePreset.ImageSize}px · {_v097ActivePreset.ImageSteps} steps");
                await PushV097PresetToBackendAsync(_v097ActivePreset);
            }

            V108SetAiPhase("Resolving image provider…", "Checking installed models and the current hardware-aware route.");
            _v108AiProvider = await V108ResolveImageProviderAsync();
            V108SetAiPhase($"Starting {_v108AiProvider.ToUpperInvariant()} generation…", "First use can spend significant time loading model weights before GPU memory rises. Elapsed time remains visible here while the backend works.");

            _lastEditedImage = await _ai.GenerateConceptAsync(prompt, outPath);
            if (!File.Exists(_lastEditedImage) || new FileInfo(_lastEditedImage).Length == 0)
                throw new InvalidOperationException("The backend returned successfully but no usable image file was produced.");

            ShowAiPreview(_lastEditedImage);
            double seconds = (DateTime.UtcNow - _v108AiStarted).TotalSeconds;
            V108SetAiResult($"AI status: completed with {_v108AiProvider} in {seconds:0}s.", $"Output: {_lastEditedImage}");
            SetStatus("Concept generated: " + _lastEditedImage);
        }
        catch (Exception ex)
        {
            string detail = V108FriendlyAiError(ex);
            double seconds = (DateTime.UtcNow - _v108AiStarted).TotalSeconds;
            V108SetAiResult($"AI status: FAILED after {seconds:0}s.", detail);
            SetStatus("AI error: " + detail);
            V108ShowAiError(detail);
        }
        finally
        {
            _v108AiBusy = false;
            V108SetAiBusy(false);
        }
    }

    async Task<string> V108ResolveImageProviderAsync()
    {
        try
        {
            string json = await _ai.GetRoutingAsync();
            using var doc = JsonDocument.Parse(json);
            var generate = doc.RootElement.GetProperty("image").GetProperty("generate");
            if (generate.TryGetProperty("provider", out var provider))
            {
                string? value = provider.GetString();
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            if (generate.TryGetProperty("error", out var error))
                throw new InvalidOperationException(error.GetString() ?? "No image provider is available.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Could not resolve an installed image-generation provider: " + V108FriendlyAiError(ex), ex);
        }
        return "auto";
    }

    void V108SetAiBusy(bool busy)
    {
        if (_v108GenerateConcept != null)
        {
            _v108GenerateConcept.Disabled = busy;
            _v108GenerateConcept.Text = busy ? "Generating Concept…" : "Generate Concept";
        }
        if (_v108CancelAi != null) _v108CancelAi.Visible = busy;
        if (_v108AiActivity != null)
        {
            _v108AiActivity.Visible = busy;
            if (busy) _v108AiActivity.Value = 4;
        }
        if (busy) _v108AiTimer?.Start(); else _v108AiTimer?.Stop();
    }

    void V108TickAiActivity()
    {
        if (!_v108AiBusy) return;
        double elapsed = (DateTime.UtcNow - _v108AiStarted).TotalSeconds;
        if (_v108AiActivity != null)
        {
            // Activity heartbeat, deliberately not presented as inference percentage because
            // the legacy synchronous backend does not expose per-step progress yet.
            double next = _v108AiActivity.Value + 7;
            _v108AiActivity.Value = next >= 96 ? 8 : next;
        }
        if (_v108AiJobStatus != null)
            _v108AiJobStatus.Text = $"AI status: {_v108AiProvider} working • {elapsed:0}s elapsed";
    }

    void V108SetAiPhase(string phase, string detail)
    {
        if (_v108AiJobStatus != null) _v108AiJobStatus.Text = "AI status: " + phase;
        if (_v108AiJobDetail != null)
        {
            _v108AiJobDetail.Text = detail;
            _v108AiJobDetail.Visible = !string.IsNullOrWhiteSpace(detail);
        }
    }

    void V108SetAiResult(string status, string detail)
    {
        if (_v108AiJobStatus != null) _v108AiJobStatus.Text = status;
        if (_v108AiJobDetail != null)
        {
            _v108AiJobDetail.Text = detail;
            _v108AiJobDetail.Visible = !string.IsNullOrWhiteSpace(detail);
        }
    }

    static string V108FriendlyAiError(Exception ex)
    {
        string message = ex.Message?.Trim() ?? "Unknown AI error.";
        try
        {
            if (message.StartsWith("{", StringComparison.Ordinal))
            {
                using var doc = JsonDocument.Parse(message);
                if (doc.RootElement.TryGetProperty("detail", out var detail))
                {
                    string? parsed = detail.GetString();
                    if (!string.IsNullOrWhiteSpace(parsed)) return parsed.Trim();
                }
            }
        }
        catch { }
        return message;
    }

    void V108ShowAiError(string detail)
    {
        var dialog = new AcceptDialog
        {
            Title = "AI generation failed",
            DialogText = detail,
            MinSize = new Vector2I(660, 260)
        };
        AddChild(dialog);
        dialog.Confirmed += dialog.QueueFree;
        dialog.Canceled += dialog.QueueFree;
        dialog.PopupCentered(new Vector2I(700, 300));
    }
}
