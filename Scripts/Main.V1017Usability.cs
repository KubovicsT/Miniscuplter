using Godot;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    sealed record V1017JobProgress(string Kind, string State, string Stage, string Detail, double Progress, string Provider);

    static readonly HttpClient V1017ProgressHttp = new() { Timeout = TimeSpan.FromSeconds(3) };

    VBoxContainer? _v1017ImagePanel;
    ProgressBar? _v1017ImageActivity;
    Label? _v1017ImageJobStatus;
    Button? _v1017EditRegion;
    Button? _v1017EnhanceRegion;
    Button? _v1017EditWhole;
    Button? _v1017CancelImage;
    bool _v1017ImageBusy;
    DateTime _v1017ImageStarted;

    TextureRect? _v1017ViewportTexture;
    Node3D? _v1017Gizmo;
    Timer? _v1017ViewportTimer;
    Label? _v1017ViewportStatus;
    bool _v1017WorldRebound;

    Label? _v1017QualityImpact;

    public void InstallV1017Usability()
    {
        InstallV1017CreativeJobHandlers();
        InstallV1017ImageEnhancement();
        InstallV1017ViewportRecovery();
        InstallV1017SettingsUpgradeHook();
        CallDeferred(nameof(V1017RepairViewport));
    }

    // MINISCULPTER_DATA is resolved once by AppDataRoot so every generated artifact shares one root.
    string V1017DataRoot() => AppDataRoot.Root;

    string V1017WorkspaceFile(string category, string prefix, string extension)
    {
        string dir = Path.Combine(V1017DataRoot(), "Workspace", category);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss_fff}{extension}");
    }

    void InstallV1017CreativeJobHandlers()
    {
        if (_v108GenerateConcept != null)
        {
            _v108GenerateConcept.Pressed -= V108GenerateConceptAsync;
            _v108GenerateConcept.Pressed += V1017GenerateConceptAsync;
            _v108GenerateConcept.TooltipText = "Generate into AIData/Workspace/2D with live backend stage feedback.";
        }
        if (_v109Generate3D != null)
        {
            _v109Generate3D.Pressed -= V109Generate3DAsync;
            _v109Generate3D.Pressed += V1017Generate3DAsync;
            _v109Generate3D.TooltipText = "Generate into AIData/Workspace/3D and import/frame the result in the active viewport.";
        }
    }

    void InstallV1017ImageEnhancement()
    {
        if (FindChild("2D Canvas Editing", true, false) is not VBoxContainer section) return;
        if (section.FindChild("Context Aware Image Editing", false, false) != null) return;

        foreach (var button in DescendantsV1015<Button>(section).Where(b => b.Text is "AI Edit Selected 2D Region" or "AI Edit Whole 2D Image"))
        {
            button.Disabled = true;
            button.Visible = false;
        }

        _v1017ImagePanel = new VBoxContainer { Name = "Context Aware Image Editing" };
        _v1017ImagePanel.AddChild(Heading("AI IMAGE EDIT"));
        _v1017ImagePanel.AddChild(new Label
        {
            Text = "Edit uses your prompt. Enhance needs no prompt: select a malformed or incoherent area and Miniscuplter reconstructs it from the surrounding image context.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        _v1017EditRegion = new Button { Text = "AI Edit Selected Region", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v1017EditRegion.Pressed += async () => await V1017RunImageEditAsync(true, false);
        _v1017ImagePanel.AddChild(_v1017EditRegion);

        _v1017EnhanceRegion = new Button { Text = "Enhance Selected Region", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v1017EnhanceRegion.TooltipText = "No prompt required. Reconstruct the selected malformed/incoherent area using the rest of the image as context.";
        _v1017EnhanceRegion.Pressed += async () => await V1017RunImageEditAsync(true, true);
        _v1017ImagePanel.AddChild(_v1017EnhanceRegion);

        _v1017EditWhole = new Button { Text = "AI Edit Whole Image", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v1017EditWhole.Pressed += async () => await V1017RunImageEditAsync(false, false);
        _v1017ImagePanel.AddChild(_v1017EditWhole);

        _v1017ImageJobStatus = new Label { Text = "AI edit status: idle", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _v1017ImagePanel.AddChild(_v1017ImageJobStatus);
        _v1017ImageActivity = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            ShowPercentage = true,
            Visible = false,
            CustomMinimumSize = new Vector2(0, 16)
        };
        _v1017ImagePanel.AddChild(_v1017ImageActivity);

        _v1017CancelImage = new Button { Text = "Cancel Image Job", Visible = false };
        _v1017CancelImage.Pressed += () =>
        {
            if (!_v1017ImageBusy) return;
            _ai.CancelCurrentRequest();
            if (_v1017ImageJobStatus != null)
                _v1017ImageJobStatus.Text = "AI edit status: cancelling and resetting the local worker…";
        };
        _v1017ImagePanel.AddChild(_v1017CancelImage);
        _v1017ImagePanel.AddChild(new HSeparator());
        section.AddChild(_v1017ImagePanel);
    }

    async Task V1017RunImageEditAsync(bool regional, bool enhance)
    {
        if (_v1017ImageBusy) return;
        string source = CurrentV1015ImageSource();
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
        {
            SetStatus("Generate, load, or choose a 2D source image first.");
            return;
        }

        string prompt = enhance ? V1017EnhancementPrompt() : (_prompt?.Text.Trim() ?? "");
        if (!enhance && prompt.Length == 0)
        {
            SetStatus("Describe the desired change in Prompt, or use Enhance Selected Region for an automatic correction.");
            return;
        }

        string? mask = null;
        if (regional)
        {
            if (_v1015ImageCanvas?.SelectionPixels is not Rect2I selection)
            {
                SetStatus("Drag a rectangle over the area you want to edit or enhance first.");
                return;
            }
            mask = V1017CreateSelectionMask(source, selection);
        }

        string output = V1017WorkspaceFile("2D", enhance ? "enhanced_region" : "image_edit", ".png");
        string expectedKind = enhance ? "2d-detail" : "2d-edit";
        _v1017ImageBusy = true;
        _v1017ImageStarted = DateTime.UtcNow;
        V1017SetImageBusy(true);
        _ = V1017PollBackendProgressAsync(expectedKind, () => _v1017ImageBusy, V1017ApplyImageProgress);

        try
        {
            V1017SetImagePhase("Checking local AI service…", 3);
            if (!await _ai.HealthAsync())
                throw new InvalidOperationException("The local AI backend did not answer. Use Repair AI Runtime in the launcher and retry.");
            if (_v097ActivePreset != null)
            {
                V1017SetImagePhase($"Applying {_v097ActivePreset.Name} quality preset…", 7);
                await PushV097PresetToBackendAsync(_v097ActivePreset);
            }

            string path;
            if (enhance)
            {
                V1017SetImagePhase("Submitting context-aware regional enhancement…", 10);
                string body = await _ai.Detail2DAsync(source, mask!, prompt, output);
                path = V1017PathFromJson(body);
            }
            else
            {
                V1017SetImagePhase(regional ? "Submitting masked image edit…" : "Submitting whole-image edit…", 10);
                path = await _ai.EditImageAsync(source, mask, prompt, output);
            }

            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidOperationException("The image backend completed but no usable output image was produced.");

            _lastEditedImage = path;
            ShowAiPreview(path);
            SyncV1015CanvasSource(path);
            _v1015ImageCanvas?.ClearSelection();
            double seconds = (DateTime.UtcNow - _v1017ImageStarted).TotalSeconds;
            if (_v1017ImageJobStatus != null)
                _v1017ImageJobStatus.Text = $"AI edit status: completed in {seconds:0}s · {Path.GetFileName(path)}";
            if (_v1017ImageActivity != null) _v1017ImageActivity.Value = 100;
            if (_v1015EditStatus != null)
                _v1015EditStatus.Text = enhance
                    ? "Enhancement generated. Review the corrected region in the center canvas."
                    : "AI edit generated. Review it in the center canvas.";
            SetStatus("2D result ready: " + path);
        }
        catch (Exception ex)
        {
            string detail = V108FriendlyAiError(ex);
            if (_v1017ImageJobStatus != null) _v1017ImageJobStatus.Text = "AI edit status: FAILED — " + detail;
            SetStatus("2D AI edit error: " + detail);
            V109ShowError(enhance ? "Enhancement failed" : "2D image edit failed", detail);
        }
        finally
        {
            _v1017ImageBusy = false;
            V1017SetImageBusy(false);
        }
    }

    string V1017CreateSelectionMask(string source, Rect2I selected)
    {
        var sourceImage = Image.LoadFromFile(source);
        if (sourceImage == null || sourceImage.IsEmpty())
            throw new InvalidDataException("The current 2D source could not be decoded.");
        var maskImage = Image.CreateEmpty(sourceImage.GetWidth(), sourceImage.GetHeight(), false, Image.Format.L8);
        maskImage.Fill(Colors.Black);
        int x0 = Math.Clamp(selected.Position.X, 0, sourceImage.GetWidth() - 1);
        int y0 = Math.Clamp(selected.Position.Y, 0, sourceImage.GetHeight() - 1);
        int x1 = Math.Clamp(selected.End.X, x0 + 1, sourceImage.GetWidth());
        int y1 = Math.Clamp(selected.End.Y, y0 + 1, sourceImage.GetHeight());
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                maskImage.SetPixel(x, y, Colors.White);
        string path = V1017WorkspaceFile("Masks", "image_mask", ".png");
        if (maskImage.SavePng(path) != Error.Ok)
            throw new IOException("Could not save the 2D selection mask into Miniscuplter AIData.");
        return path;
    }

    static string V1017EnhancementPrompt() =>
        "Context-aware corrective enhancement. Inspect the selected region in the context of the complete source image and reconstruct only that selected area so it is structurally, anatomically, mechanically and stylistically coherent with the surrounding subject. Correct malformed, melted, duplicated, impossible, mismatched or low-detail geometry and details. Preserve the identity, pose and design of the subject. Match the surrounding perspective, scale, lighting, materials, colors and visual language. Do not modify anything outside the selected region and do not add unrelated objects, text, watermarks or logos.";

    static string V1017PathFromJson(string body)
    {
        using var doc = JsonDocument.Parse(body);
        string? path = doc.RootElement.TryGetProperty("path", out var p) ? p.GetString() : null;
        if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Backend returned no output path.");
        return path;
    }

    void V1017SetImageBusy(bool busy)
    {
        if (_v1017EditRegion != null) _v1017EditRegion.Disabled = busy;
        if (_v1017EnhanceRegion != null) _v1017EnhanceRegion.Disabled = busy;
        if (_v1017EditWhole != null) _v1017EditWhole.Disabled = busy;
        if (_v1017CancelImage != null) _v1017CancelImage.Visible = busy;
        if (_v1017ImageActivity != null)
        {
            _v1017ImageActivity.Visible = busy;
            if (busy) _v1017ImageActivity.Value = 2;
        }
    }

    void V1017SetImagePhase(string text, double progress)
    {
        if (_v1017ImageJobStatus != null) _v1017ImageJobStatus.Text = "AI edit status: " + text;
        if (_v1017ImageActivity != null) _v1017ImageActivity.Value = progress;
    }

    async void V1017GenerateConceptAsync()
    {
        if (_v108AiBusy) return;
        string prompt = _prompt?.Text.Trim() ?? "";
        if (prompt.Length == 0)
        {
            V108SetAiResult("AI status: enter a prompt first.", "Nothing was sent to the backend.");
            return;
        }

        string output = V1017WorkspaceFile("2D", "concept", ".png");
        _v108AiBusy = true;
        _v108AiStarted = DateTime.UtcNow;
        _v108AiProvider = "auto";
        V1017SetConceptBusy(true);
        _ = V1017PollBackendProgressAsync("2d-generate", () => _v108AiBusy, V1017ApplyConceptProgress);

        try
        {
            V108SetAiPhase("Checking local AI service…", "Verifying the local backend before generation.");
            if (!await _ai.HealthAsync())
                throw new InvalidOperationException("The local AI backend did not answer its health check. Use Repair AI Runtime in the launcher and restart the editor.");
            if (_v097ActivePreset != null)
            {
                V108SetAiPhase("Applying quality preset…", $"Preset: {_v097ActivePreset.Name} · {_v097ActivePreset.ImageSize}px · {_v097ActivePreset.ImageSteps} steps");
                await PushV097PresetToBackendAsync(_v097ActivePreset);
            }
            V108SetAiPhase("Resolving image provider…", "Checking installed local models and Settings → Models routing.");
            _v108AiProvider = await V108ResolveImageProviderAsync();
            _lastEditedImage = await _ai.GenerateConceptAsync(prompt, output);
            if (!File.Exists(_lastEditedImage) || new FileInfo(_lastEditedImage).Length == 0)
                throw new InvalidOperationException("The backend returned successfully but no usable image file was produced.");
            ShowAiPreview(_lastEditedImage);
            SyncV1015CanvasSource(_lastEditedImage);
            double seconds = (DateTime.UtcNow - _v108AiStarted).TotalSeconds;
            V108SetAiResult($"AI status: completed with {_v108AiProvider} in {seconds:0}s.", $"Output: {_lastEditedImage}");
            if (_v108AiActivity != null) _v108AiActivity.Value = 100;
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
            V1017SetConceptBusy(false);
        }
    }

    void V1017SetConceptBusy(bool busy)
    {
        _v108AiTimer?.Stop();
        if (_v108GenerateConcept != null)
        {
            _v108GenerateConcept.Disabled = busy;
            _v108GenerateConcept.Text = busy ? "Generating Concept…" : "Generate Concept";
        }
        if (_v108CancelAi != null) _v108CancelAi.Visible = busy;
        if (_v108AiActivity != null)
        {
            _v108AiActivity.ShowPercentage = true;
            _v108AiActivity.Visible = busy;
            if (busy) _v108AiActivity.Value = 2;
        }
    }

    async void V1017Generate3DAsync()
    {
        if (_v1093DBusy) return;
        string image = V1011Approved2DSource();
        if (string.IsNullOrWhiteSpace(image) || !File.Exists(image))
        {
            SetV1093DResult("3D status: no accepted 2D baseline.", "Return to 2D and choose Accept Current Image as Baseline first.");
            return;
        }

        string output = V1017WorkspaceFile("3D", "ai_part", ".stl");
        string prompt = _prompt?.Text.Trim() ?? "";
        _v1093DBusy = true;
        _v1093DStarted = DateTime.UtcNow;
        _v1093DProvider = _v098Routes.Quality3D;
        V1017Set3DBusy(true);
        _ = V1017PollBackendProgressAsync("3d-generate", () => _v1093DBusy, V1017Apply3DProgress);

        try
        {
            SetV1093DPhase("Checking local AI service…", "Accepted source: " + Path.GetFileName(image), 3);
            if (!await _ai.HealthAsync())
                throw new InvalidOperationException("The local AI backend did not answer its health check. Use Repair AI Runtime in Launcher.");
            if (_v097ActivePreset != null)
            {
                SetV1093DPhase("Applying quality preset…", $"{_v097ActivePreset.Name} · {_v097ActivePreset.ShapeSteps} 3D steps", 7);
                await PushV097PresetToBackendAsync(_v097ActivePreset);
            }
            SetV1093DPhase("Resolving 3D provider…", "Using Settings → Models routing preference.", 10);
            if (_v1093DProvider.Equals("auto", StringComparison.OrdinalIgnoreCase))
                _v1093DProvider = await ResolveV109Quality3DProviderAsync();

            string path = await _ai.Generate3DRoutedAsync(image, prompt, output, "quality", _v1093DProvider);
            SetV1093DPhase("Validating generated STL…", path, 94);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidOperationException("The 3D provider returned without a usable STL file.");

            var mesh = MeshIO.LoadStl(path);
            string meshSummary = V1017ValidateMeshForViewport(mesh);
            SetV1093DPhase("Importing and framing mesh…", meshSummary, 97);
            AddMeshObject(mesh, $"AI 3D — {_v1093DProvider}");
            if (_selected?.MaterialOverride is StandardMaterial3D material)
                material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
            V1017RepairViewport();
            FrameSelected();
            V1017UpdateGizmo();
            CallDeferred(nameof(V1017FrameAndVerifySelected));

            double seconds = (DateTime.UtcNow - _v1093DStarted).TotalSeconds;
            SetV1093DResult(
                $"3D status: completed with {_v1093DProvider} in {seconds:0}s.",
                $"{meshSummary}\nOutput: {path}\nViewport: imported, selected and framed.");
            if (_v1093DActivity != null) _v1093DActivity.Value = 100;
            SetStatus("AI 3D part imported and framed in the viewport.");
        }
        catch (Exception ex)
        {
            double seconds = (DateTime.UtcNow - _v1093DStarted).TotalSeconds;
            string detail = V108FriendlyAiError(ex);
            SetV1093DResult($"3D status: FAILED after {seconds:0}s.", detail);
            SetStatus("3D AI error: " + detail);
            V109ShowError("2D → 3D generation failed", detail);
        }
        finally
        {
            _v1093DBusy = false;
            V1017Set3DBusy(false);
        }
    }

    static string V1017ValidateMeshForViewport(ArrayMesh mesh)
    {
        if (mesh.GetSurfaceCount() == 0)
            throw new InvalidDataException("The generated STL contains no renderable surfaces.");
        long vertices = 0;
        for (int s = 0; s < mesh.GetSurfaceCount(); s++)
        {
            var arrays = mesh.SurfaceGetArrays(s);
            var points = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            vertices += points.Length;
            foreach (var p in points)
            {
                if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z))
                    throw new InvalidDataException("The generated mesh contains non-finite vertex coordinates and cannot be displayed safely.");
            }
        }
        if (vertices < 3) throw new InvalidDataException("The generated mesh contains too few vertices to display.");
        var box = mesh.GetAabb();
        if (!float.IsFinite(box.Size.X) || !float.IsFinite(box.Size.Y) || !float.IsFinite(box.Size.Z) || box.Size.LengthSquared() <= 1e-12f)
            throw new InvalidDataException("The generated mesh has invalid or zero-size bounds.");
        return $"Mesh: {vertices:N0} vertices · bounds {box.Size.X:0.###} × {box.Size.Y:0.###} × {box.Size.Z:0.###} scene units";
    }

    void V1017Set3DBusy(bool busy)
    {
        _v1093DTimer?.Stop();
        if (_v109Cancel3D != null) _v109Cancel3D.Visible = busy;
        if (_v1093DActivity != null)
        {
            _v1093DActivity.ShowPercentage = true;
            _v1093DActivity.Visible = busy;
            if (busy) _v1093DActivity.Value = 2;
        }
        if (_v109Generate3D != null)
        {
            bool hasBaseline = !string.IsNullOrWhiteSpace(V1011Approved2DSource());
            _v109Generate3D.Disabled = busy || !hasBaseline;
            _v109Generate3D.Text = busy
                ? "Generating 3D Part…"
                : hasBaseline ? "Generate 3D from Accepted Baseline" : "Accept a 2D Baseline First";
        }
    }

    async Task V1017PollBackendProgressAsync(string expectedKind, Func<bool> busy, Action<V1017JobProgress> apply)
    {
        await Task.Delay(250);
        long lastSequence = 0;
        int failures = 0;
        string? jobId = null;

        while (busy())
        {
            try
            {
                jobId ??= _ai.ActiveJobId;
                V1017JobProgress? item = null;

                if (!string.IsNullOrWhiteSpace(jobId))
                {
                    AiJobProgress progress = await _ai.GetJobProgressAsync(jobId);
                    item = new V1017JobProgress(
                        progress.Kind,
                        progress.State,
                        progress.Stage,
                        progress.Detail,
                        Math.Clamp(progress.Progress, 0, 100),
                        progress.Provider);
                }
                else
                {
                    using var response = await V1017ProgressHttp.GetAsync(_ai.BackendUrl + "/job-progress/current");
                    if (response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;
                        string kind = root.TryGetProperty("kind", out var k) ? k.GetString() ?? "" : "";
                        string state = root.TryGetProperty("state", out var st) ? st.GetString() ?? "running" : "running";
                        string stage = root.TryGetProperty("stage", out var sg) ? sg.GetString() ?? "working" : "working";
                        string detail = root.TryGetProperty("detail", out var dt) ? dt.GetString() ?? "" : "";
                        string provider = root.TryGetProperty("provider", out var pr) && pr.ValueKind != JsonValueKind.Null ? pr.GetString() ?? "" : "";
                        double value = root.TryGetProperty("progress", out var pg) && pg.TryGetDouble(out var parsed) ? parsed : 0;
                        long sequence = root.TryGetProperty("sequence", out var seq) && seq.TryGetInt64(out var number) ? number : 0;
                        item = new V1017JobProgress(kind, state, stage, detail, Math.Clamp(value, 0, 100), provider);
                        if (sequence <= lastSequence) item = null;
                        lastSequence = sequence;
                    }
                }

                if (item != null && item.Kind.Equals(expectedKind, StringComparison.OrdinalIgnoreCase))
                {
                    apply(item);
                    failures = 0;
                }
            }
            catch (Exception ex)
            {
                failures++;
                if (failures == 1)
                    GD.PushWarning($"AI progress polling failed for {expectedKind}: {ex.Message}");
                if (failures >= 3)
                {
                    Label? label = expectedKind.StartsWith("2d", StringComparison.OrdinalIgnoreCase)
                        ? _v1017ImageJobStatus
                        : expectedKind.StartsWith("3d", StringComparison.OrdinalIgnoreCase)
                            ? _v1093DStatus
                            : _v108AiJobStatus;
                    if (label != null)
                        label.Text = $"AI status: backend progress temporarily unavailable; request still running… ({ex.Message})";
                }
            }

            await Task.Delay(600);
        }
    }

    static string V1017StageName(string stage) => stage switch
    {
        "queued" => "Queued",
        "resolving_provider" => "Resolving model/provider",
        "preparing_runtime" => "Preparing runtime / freeing VRAM",
        "loading_model" => "Loading model weights",
        "preparing_inputs" => "Preparing inputs",
        "running_inference" => "Running AI inference",
        "decoding_output" => "Decoding model output",
        "saving_result" => "Saving result",
        "validating_output" => "Validating result",
        "cleanup" => "Releasing model / cleaning up",
        "completed" => "Completed",
        "failed" => "Failed",
        _ => stage.Replace('_', ' ')
    };

    void V1017ApplyImageProgress(V1017JobProgress p)
    {
        if (_v1017ImageActivity != null) _v1017ImageActivity.Value = p.Progress;
        if (_v1017ImageJobStatus != null)
        {
            double elapsed = (DateTime.UtcNow - _v1017ImageStarted).TotalSeconds;
            string provider = string.IsNullOrWhiteSpace(p.Provider) ? "" : $" · {p.Provider}";
            string detail = string.IsNullOrWhiteSpace(p.Detail) ? "" : $" — {p.Detail}";
            _v1017ImageJobStatus.Text = $"AI edit status: {V1017StageName(p.Stage)}{provider} · {elapsed:0}s{detail}";
        }
    }

    void V1017ApplyConceptProgress(V1017JobProgress p)
    {
        if (_v108AiActivity != null) _v108AiActivity.Value = p.Progress;
        if (_v108AiJobStatus != null)
        {
            double elapsed = (DateTime.UtcNow - _v108AiStarted).TotalSeconds;
            string provider = string.IsNullOrWhiteSpace(p.Provider) ? "" : $" · {p.Provider}";
            _v108AiJobStatus.Text = $"AI status: {V1017StageName(p.Stage)}{provider} · {elapsed:0}s";
        }
        if (_v108AiJobDetail != null && !string.IsNullOrWhiteSpace(p.Detail))
        {
            _v108AiJobDetail.Text = p.Detail;
            _v108AiJobDetail.Visible = true;
        }
    }

    void V1017Apply3DProgress(V1017JobProgress p)
    {
        if (_v1093DActivity != null) _v1093DActivity.Value = p.Progress;
        if (_v1093DStatus != null)
        {
            double elapsed = (DateTime.UtcNow - _v1093DStarted).TotalSeconds;
            string provider = string.IsNullOrWhiteSpace(p.Provider) ? "" : $" · {p.Provider}";
            _v1093DStatus.Text = $"3D status: {V1017StageName(p.Stage)}{provider} · {elapsed:0}s";
        }
        if (_v1093DDetail != null && !string.IsNullOrWhiteSpace(p.Detail))
        {
            _v1093DDetail.Text = p.Detail;
            _v1093DDetail.Visible = true;
        }
    }

    void InstallV1017ViewportRecovery()
    {
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host ||
            FindChild("Viewport", true, false) is not SubViewport)
            return;

        // v1.0.18 uses the native SubViewportContainer surface. The historical
        // "3D Viewport Texture" overlay is deliberately not created because it could
        // obscure or desynchronize the actual 3D render target.
        var tabs = (host.GetParent() as HSplitContainer)?.GetChildren().OfType<TabContainer>().FirstOrDefault();
        if (tabs != null) tabs.TabChanged += V1017WorkflowTabChanged;

        if (FindChild("3D", true, false) is VBoxContainer threeD)
        {
            _v1017ViewportStatus = new Label { Text = "Viewport: initializing…", AutowrapMode = TextServer.AutowrapMode.WordSmart };
            threeD.AddChild(_v1017ViewportStatus);
            threeD.MoveChild(_v1017ViewportStatus, Math.Min(2, threeD.GetChildCount() - 1));
        }

        _v1017ViewportTimer = new Timer { WaitTime = .5, OneShot = false };
        _v1017ViewportTimer.Timeout += () =>
        {
            V1017SyncViewport();
            V1017UpdateGizmo();
            V1017UpdateViewportStatus();
        };
        AddChild(_v1017ViewportTimer);
        _v1017ViewportTimer.Start();
    }

    void V1017WorkflowTabChanged(long tab)
    {
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host) return;
        var tabs = (host.GetParent() as HSplitContainer)?.GetChildren().OfType<TabContainer>().FirstOrDefault();
        string title = tabs != null && tabs.GetTabCount() > 0
            ? tabs.GetTabTitle(Math.Clamp((int)tab, 0, tabs.GetTabCount() - 1))
            : "";
        bool twoD = title.Equals("2D", StringComparison.OrdinalIgnoreCase);
        if (_v1017ViewportTexture != null) _v1017ViewportTexture.Visible = !twoD;
        if (!twoD)
        {
            if (_v1015ImageCanvas != null) _v1015ImageCanvas.Visible = false;
            if (_v1015CanvasHint != null) _v1015CanvasHint.Visible = false;
            V1017RepairViewport();
            if (_selected != null) CallDeferred(nameof(V1017FrameAndVerifySelected));
        }
    }

    void V1017RepairViewport()
    {
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host ||
            FindChild("Viewport", true, false) is not SubViewport sub || _world == null) return;

        host.Visible = true;
        host.Stretch = true;
        host.Modulate = Colors.White;
        host.SelfModulate = Colors.White;
        sub.Disable3D = false;
        sub.TransparentBg = false;
        sub.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        sub.OwnWorld3D = true;

        // v1.0.9 enabled OwnWorld3D after the Node3D world/camera already existed. Reparent once
        // after ownership is established so the world, lights and camera register against the
        // SubViewport's active World3D instead of remaining attached to the prior inherited world.
        if (!_v1017WorldRebound && ReferenceEquals(_world.GetParent(), sub))
        {
            sub.RemoveChild(_world);
            sub.AddChild(_world);
            _v1017WorldRebound = true;
        }

        V1017SyncViewport();
        if (_camera != null)
        {
            _camera.Visible = true;
            _camera.Current = true;
            _camera.Near = .01f;
            _camera.Far = 10000f;
            UpdateCamera();
        }

        foreach (var obj in _objects)
        {
            if (!IsInstanceValid(obj)) continue;
            obj.Visible = true;
            obj.Layers = 1;
            if (obj.MaterialOverride is StandardMaterial3D material)
                material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        }

        if (_v109GridRoot == null || !IsInstanceValid(_v109GridRoot) ||
            !_v109GridRoot.Name.ToString().Contains("v1.0.17", StringComparison.Ordinal))
            V1017RebuildGrid();
        if (_v1017Gizmo == null || !IsInstanceValid(_v1017Gizmo)) V1017BuildGizmo();

        if (_world.GetChildren().OfType<WorldEnvironment>().FirstOrDefault()?.Environment is Godot.Environment env)
        {
            env.BackgroundMode = Godot.Environment.BGMode.Color;
            env.BackgroundColor = new Color(.025f, .030f, .040f);
            env.AmbientLightSource = Godot.Environment.AmbientSource.Color;
            env.AmbientLightColor = new Color(.52f, .54f, .60f);
            env.AmbientLightEnergy = 1.05f;
        }
    }

    void V1017SyncViewport()
    {
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host ||
            FindChild("Viewport", true, false) is not SubViewport sub) return;
        Vector2 size = host.Size;
        var target = new Vector2I(Math.Max(1, (int)Math.Round(size.X)), Math.Max(1, (int)Math.Round(size.Y)));
        if (sub.Size != target) sub.Size = target;
        sub.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        // The SubViewportContainer displays the SubViewport directly in v1.0.18.
    }

    void V1017RebuildGrid()
    {
        if (_world == null) return;
        bool visible = _v109GridToggle?.ButtonPressed ?? true;
        if (_v109GridRoot != null && IsInstanceValid(_v109GridRoot)) _v109GridRoot.QueueFree();
        _v109GridRoot = new Node3D { Name = "Viewport Grid v1.0.17", Visible = visible };
        _world.AddChild(_v109GridRoot);

        var ground = new MeshInstance3D
        {
            Name = "Grid ground",
            Mesh = new PlaneMesh { Size = new Vector2(500, 500) },
            Position = new Vector3(0, -.03f, 0),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(.055f, .061f, .073f),
                Roughness = 1f,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            }
        };
        _v109GridRoot.AddChild(ground);
        AddV1015GridBars(10f, .16f, new Color(.30f, .34f, .40f), false);
        AddV1015GridBars(50f, .42f, new Color(.52f, .56f, .64f), true);
        AddV1015AxisBar(true, .72f, new Color(.92f, .28f, .24f));
        AddV1015AxisBar(false, .72f, new Color(.25f, .55f, 1f));
        if (_v109GridToggle != null) _v109GridToggle.ButtonPressed = visible;
    }

    void V1017BuildGizmo()
    {
        if (_world == null) return;
        if (_v1017Gizmo != null && IsInstanceValid(_v1017Gizmo)) _v1017Gizmo.QueueFree();
        _v1017Gizmo = new Node3D { Name = "Selection Gizmo v1.0.17", Visible = false };
        _world.AddChild(_v1017Gizmo);

        void Axis(string name, Vector3 size, Vector3 position, Color color)
        {
            var material = new StandardMaterial3D
            {
                AlbedoColor = color,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            };
            _v1017Gizmo.AddChild(new MeshInstance3D
            {
                Name = name,
                Mesh = new BoxMesh { Size = size },
                Position = position,
                MaterialOverride = material
            });
        }

        Axis("X handle", new Vector3(1f, .045f, .045f), new Vector3(.5f, 0, 0), new Color(.95f, .22f, .18f));
        Axis("Y handle", new Vector3(.045f, 1f, .045f), new Vector3(0, .5f, 0), new Color(.24f, .90f, .35f));
        Axis("Z handle", new Vector3(.045f, .045f, 1f), new Vector3(0, 0, .5f), new Color(.20f, .48f, 1f));
        _v1017Gizmo.AddChild(new MeshInstance3D
        {
            Name = "Gizmo origin",
            Mesh = new SphereMesh { Radius = .075f, Height = .15f, RadialSegments = 16, Rings = 8 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = Colors.White,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            }
        });
    }

    void V1017UpdateGizmo()
    {
        if (_v1017Gizmo == null || !IsInstanceValid(_v1017Gizmo)) return;
        if (_selected == null || !IsInstanceValid(_selected) || !_selected.Visible)
        {
            _v1017Gizmo.Visible = false;
            return;
        }
        var aabb = _selected.GetAabb();
        Vector3 center = _selected.GlobalTransform * (aabb.Position + aabb.Size * .5f);
        Vector3 s = _selected.Scale;
        float extent = Math.Max(aabb.Size.X * Math.Abs(s.X), Math.Max(aabb.Size.Y * Math.Abs(s.Y), aabb.Size.Z * Math.Abs(s.Z)));
        float length = Math.Clamp(extent * .30f, 3f, 30f);
        _v1017Gizmo.Position = center;
        _v1017Gizmo.Scale = Vector3.One * length;
        _v1017Gizmo.Visible = true;
    }

    void V1017FrameAndVerifySelected()
    {
        if (_selected == null) return;
        FrameSelected();
        V1017SyncViewport();
        V1017UpdateGizmo();
        if (_v1017ViewportTexture != null) _v1017ViewportTexture.Visible = true;
        if (_v109GridRoot != null && (_v109GridToggle?.ButtonPressed ?? true)) _v109GridRoot.Visible = true;
        V1017UpdateViewportStatus();
    }

    void V1017UpdateViewportStatus()
    {
        if (_v1017ViewportStatus == null) return;
        if (FindChild("Viewport", true, false) is not SubViewport sub)
        {
            _v1017ViewportStatus.Text = "Viewport: unavailable";
            return;
        }
        string selected = _selected == null ? "none" : _selected.Name.ToString();
        _v1017ViewportStatus.Text = $"Viewport: {sub.Size.X}×{sub.Size.Y} · 3D {(sub.Disable3D ? "disabled" : "active")} · {_objects.Count} scene object(s) · selected: {selected}";
    }

    void InstallV1017SettingsUpgradeHook()
    {
        var root = GetChildren().OfType<VBoxContainer>().FirstOrDefault();
        var toolbar = root?.GetChildren().OfType<HBoxContainer>().FirstOrDefault();
        foreach (var button in toolbar?.GetChildren().OfType<Button>().Where(b => b.Text == "Settings") ?? Enumerable.Empty<Button>())
            button.Pressed += () => CallDeferred(nameof(V1017UpgradeSettingsWindow));
        if (_v109SettingsWindow != null) CallDeferred(nameof(V1017UpgradeSettingsWindow));
    }

    void V1017UpgradeSettingsWindow()
    {
        if (_v109SettingsWindow == null || !IsInstanceValid(_v109SettingsWindow)) return;
        _v109SettingsWindow.Title = "Miniscuplter Settings";
        if (_v109SettingsWindow.FindChild("TabContainer", true, false) is not TabContainer tabs) return;

        for (int i = tabs.GetTabCount() - 1; i >= 0; i--)
        {
            string title = tabs.GetTabTitle(i);
            if (!title.Contains("Advanced Quality", StringComparison.OrdinalIgnoreCase)) continue;
            var page = tabs.GetChild(i);
            tabs.RemoveChild(page);
            page.QueueFree();
        }

        int qualityIndex = -1;
        for (int i = 0; i < tabs.GetTabCount(); i++)
        {
            if (tabs.GetTabTitle(i).Equals("Quality", StringComparison.OrdinalIgnoreCase))
            {
                qualityIndex = i;
                break;
            }
        }

        if (qualityIndex >= 0 && tabs.GetChild(qualityIndex).FindChild("V1017 Quality Panel", true, false) == null)
        {
            var old = tabs.GetChild(qualityIndex);
            tabs.RemoveChild(old);
            old.QueueFree();
            var scroll = new ScrollContainer
            {
                Name = "Quality",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            var box = new VBoxContainer { Name = "V1017 Quality Panel", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            scroll.AddChild(box);
            tabs.AddChild(scroll);
            tabs.MoveChild(scroll, qualityIndex);
            tabs.SetTabTitle(qualityIndex, "Quality");
            V1017BuildQualityPanel(box);
        }
        else
        {
            V1017UpdateQualityImpact();
        }

        bool hasStorage = Enumerable.Range(0, tabs.GetTabCount())
            .Any(i => tabs.GetTabTitle(i).Equals("Storage", StringComparison.OrdinalIgnoreCase));
        if (!hasStorage) V1017AddStorageTab(tabs);

        for (int i = 0; i < tabs.GetTabCount(); i++)
        {
            if (!tabs.GetTabTitle(i).Equals("Viewport", StringComparison.OrdinalIgnoreCase)) continue;
            if (tabs.GetChild(i).FindChild("Repair 3D Viewport", true, false) != null) break;
            if (tabs.GetChild(i) is Container view)
            {
                var repair = new Button { Text = "Repair 3D Viewport", Name = "Repair 3D Viewport" };
                repair.Pressed += () =>
                {
                    V1017RepairViewport();
                    V1017FrameAndVerifySelected();
                    SetStatus("3D viewport rebuilt, rebound and reframed.");
                };
                view.AddChild(repair);
                view.AddChild(new Label
                {
                    Text = "If a graphics-driver or window event leaves the canvas blank, Repair explicitly rebinds the 3D world, render texture, camera, grid and selected object.",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart
                });
            }
            break;
        }
    }

    void V1017BuildQualityPanel(Container box)
    {
        box.AddChild(Heading("QUALITY PRESETS"));
        box.AddChild(new Label
        {
            Text = "Selecting a preset shows every value it changes. Higher resolution/steps cost more time and memory; smaller voxel pitches retain more geometry detail but are much heavier.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        if (_v097Presets.Count == 0) LoadV097Presets();
        _v097PresetSelect = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddChild(_v097PresetSelect);
        _v097PresetSelect.ItemSelected += i =>
        {
            if (_v097LoadingUi) return;
            V097SelectPreset((int)i, true);
            V1017UpdateQualityImpact();
        };
        _v097ActiveSummary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        box.AddChild(_v097ActiveSummary);
        _v1017QualityImpact = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        box.AddChild(_v1017QualityImpact);
        box.AddChild(new HSeparator());

        box.AddChild(Heading("2D IMAGE"));
        _v097ImageSize = AddV097Spin(box, "Resolution (square pixels)", 256, 1536, 64, 512, " px");
        _v097ImageSteps = AddV097Spin(box, "Inference steps", 4, 100, 1, 24);
        _v097ImageGuidance = AddV097Spin(box, "Guidance / CFG", 1, 20, .1, 7);
        _v097EditStrength = AddV097Spin(box, "Image edit strength", .05, .95, .01, .58);
        _v097MaxInput = AddV097Spin(box, "Maximum input dimension", 512, 8192, 128, 2048, " px");

        box.AddChild(Heading("3D GENERATION"));
        _v097ShapeSteps = AddV097Spin(box, "Hunyuan / shape inference steps", 8, 100, 1, 30);

        box.AddChild(Heading("SCULPT / CLEANUP / ANALYSIS"));
        _v097RemeshVoxel = AddV097Spin(box, "Sculpt/remesh voxel pitch", .04, 5, .01, .28, " mm");
        _v097RepairVoxel = AddV097Spin(box, "Repair/final-bake voxel pitch", .04, 5, .01, .30, " mm");
        _v097MaxVoxelCells = AddV097Spin(box, "Voxel memory safety budget", 1_000_000, 2_000_000_000, 1_000_000, 100_000_000, " cells");
        _v097ThicknessSamples = AddV097Spin(box, "Thickness analysis samples", 100, 100_000, 500, 12_000);
        _v097SmartViews = AddV097Spin(box, "Smart Select rendered views", 2, 12, 1, 6);
        _v097SmartSize = AddV097Spin(box, "Smart Select render resolution", 128, 1024, 32, 352, " px");

        box.AddChild(new HSeparator());
        box.AddChild(Heading("CUSTOM PRESET"));
        _v097Name = new LineEdit { PlaceholderText = "Preset name" };
        box.AddChild(_v097Name);

        var row1 = new HBoxContainer();
        var create = new Button { Text = "Create Custom", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var clone = new Button { Text = "Clone Preset", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        create.Pressed += () =>
        {
            if (_v097ActivePreset?.BuiltIn == true && _v097Name != null &&
                _v097Name.Text.Trim().Equals(_v097ActivePreset.Name, StringComparison.OrdinalIgnoreCase))
                _v097Name.Text = _v097ActivePreset.Name + " Custom";
            V097CreateCustom();
            V1017UpdateQualityImpact();
        };
        clone.Pressed += V1017ClonePreset;
        row1.AddChild(create);
        row1.AddChild(clone);
        box.AddChild(row1);

        var row2 = new HBoxContainer();
        var rename = new Button { Text = "Rename Custom", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v097UpdateCustom = new Button { Text = "Save Changes", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v097DeleteCustom = new Button { Text = "Delete Custom" };
        rename.Pressed += V1017RenamePreset;
        _v097UpdateCustom.Pressed += () => { V097UpdateCustom(); V1017UpdateQualityImpact(); };
        _v097DeleteCustom.Pressed += () => { V097DeleteCustom(); V1017UpdateQualityImpact(); };
        row2.AddChild(rename);
        row2.AddChild(_v097UpdateCustom);
        row2.AddChild(_v097DeleteCustom);
        box.AddChild(row2);

        box.AddChild(new Label
        {
            Text = "Built-in Low / Medium / High / Ultra presets are immutable. Clone one or edit its displayed values and choose Create Custom. Custom presets can be renamed, changed and deleted here.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        RefreshV097PresetList();
        int initial = Math.Max(0, _v097Presets.FindIndex(p => p.Id == (_v097ActivePreset?.Id ?? "builtin-medium")));
        V097SelectPreset(initial, false);
        V1017UpdateQualityImpact();
    }

    void V1017ClonePreset()
    {
        if (_v097ActivePreset == null || _v097Name == null) return;
        _v097Name.Text = _v097ActivePreset.Name + " Copy";
        V097CreateCustom();
        V1017UpdateQualityImpact();
    }

    void V1017RenamePreset()
    {
        if (_v097ActivePreset == null || _v097ActivePreset.BuiltIn)
        {
            SetStatus("Built-in presets cannot be renamed. Clone the preset first.");
            return;
        }
        if (_v097Name == null || string.IsNullOrWhiteSpace(_v097Name.Text))
        {
            SetStatus("Enter a custom preset name first.");
            return;
        }
        V097UpdateCustom();
        V1017UpdateQualityImpact();
    }

    void V1017UpdateQualityImpact()
    {
        if (_v1017QualityImpact == null || _v097ActivePreset == null) return;
        var p = _v097ActivePreset;
        var medium = _v097Presets.FirstOrDefault(x => x.Id == "builtin-medium") ?? p;
        double pixels = (double)p.ImageSize * p.ImageSize /
                        Math.Max(1.0, (double)medium.ImageSize * medium.ImageSize);
        string speed = p.ImageSteps > medium.ImageSteps || p.ShapeSteps > medium.ShapeSteps || p.ImageSize > medium.ImageSize
            ? "more compute / slower"
            : p.ImageSteps < medium.ImageSteps || p.ShapeSteps < medium.ShapeSteps || p.ImageSize < medium.ImageSize
                ? "less compute / faster"
                : "balanced baseline";
        _v1017QualityImpact.Text =
            $"Selected preset: {p.Name}\n" +
            $"2D: {p.ImageSize}×{p.ImageSize}px · {p.ImageSteps} steps · CFG {p.ImageGuidance:0.0} · edit strength {p.ImageEditStrength:0.00} · max input {p.MaxInputPx}px\n" +
            $"3D: {p.ShapeSteps} shape steps\n" +
            $"Geometry: remesh {p.RemeshVoxelMm:0.00} mm · repair {p.RepairVoxelMm:0.00} mm · voxel budget {p.MaxVoxelCells:N0}\n" +
            $"Analysis: {p.ThicknessSamples:N0} thickness samples · Smart Select {p.SmartSelectViews} views at {p.SmartSelectRenderSize}px\n" +
            $"Compared with Medium: {pixels:0.00}× 2D pixel workload before step count; image steps {medium.ImageSteps}→{p.ImageSteps}; 3D steps {medium.ShapeSteps}→{p.ShapeSteps}; remesh {medium.RemeshVoxelMm:0.00}→{p.RemeshVoxelMm:0.00} mm. Expected profile: {speed}.";
    }

    void V1017AddStorageTab(TabContainer tabs)
    {
        var scroll = new ScrollContainer
        {
            Name = "Storage",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(box);
        tabs.AddChild(scroll);
        tabs.SetTabTitle(tabs.GetTabCount() - 1, "Storage");

        box.AddChild(Heading("MINISCULPTER DATA ROOT"));
        box.AddChild(new Label
        {
            Text = V1017DataRoot(),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        box.AddChild(new Label
        {
            Text = "Generated 2D images, masks, generated 3D files, job artifacts, Godot user data, temporary files and common AI caches are routed under the launcher-selected Miniscuplter data root instead of Windows AppData/TEMP. Legacy Godot user data is copy-migrated once and the old location is left untouched as a safety backup.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        var open = new Button { Text = "Open Miniscuplter Data Folder" };
        open.Pressed += () =>
        {
            Directory.CreateDirectory(V1017DataRoot());
            OS.ShellOpen(V1017DataRoot());
        };
        box.AddChild(open);
        box.AddChild(new Label
        {
            Text = "Working files: " + Path.Combine(V1017DataRoot(), "Workspace") +
                   "\nTemporary files: " + Path.Combine(V1017DataRoot(), "Temp") +
                   "\nCaches: " + Path.Combine(V1017DataRoot(), "Cache"),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
    }
}
