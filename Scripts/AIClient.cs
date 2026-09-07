using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetHttpClient = System.Net.Http.HttpClient;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Miniscuplter;

public record ReferenceResult(string Title, string PageUrl, string? ThumbnailUrl);
public record AiComponentInfo(string Id, string Name, string Kind, bool Installed, double EstimatedGb, string Description, string? Path);
public record AiHardwareInfo(string? Gpu, int VramMb, bool CudaAvailable, string RecommendedProfile);
public record AiComponentStatus(AiHardwareInfo Hardware, List<AiComponentInfo> Components, string DataRoot);
public record AiJobProgress(string JobId, string Kind, string State, string Stage, string Detail, double Progress, string Provider, bool Active, bool CancelRequested, long Sequence);

public sealed class AIClient
{
    readonly NetHttpClient _http = new() { Timeout = TimeSpan.FromMinutes(90) };
    readonly object _cancelLock = new();
    readonly SemaphoreSlim _jobGate = new(1, 1);
    CancellationTokenSource? _activeRequest;
    Task _cancelRecovery = Task.CompletedTask;
    string? _activeJobId;
    string? _lastJobId;

    public string? ActiveJobId { get { lock (_cancelLock) return _activeJobId; } }
    public string? LastJobId { get { lock (_cancelLock) return _lastJobId; } }

    /// <summary>
    /// The packaged editor owns the local backend process. A cancelled HTTP request alone does
    /// not stop a synchronous FastAPI inference handler, so Main supplies a recovery callback
    /// that kills/restarts the owned backend process tree. New backend work waits for that
    /// recovery to finish before it can start.
    /// </summary>
    public Func<Task>? CancellationRecoveryHandler { get; set; }

    public string BackendUrl { get; set; } = "http://127.0.0.1:7868";
    public bool InternetReferencesEnabled { get; set; } = true;

    // v0.9.8 role routing. "auto" lets the backend choose from installed specialists.
    public string ImageGenerateProvider { get; set; } = "auto";
    public string ImageEditProvider { get; set; } = "auto";
    public string ImageDetailProvider { get; set; } = "auto";
    public string Fast3DProvider { get; set; } = "auto";
    public string Quality3DProvider { get; set; } = "auto";
    public string Detail3DProvider { get; set; } = "auto";
    public string Structured3DProvider { get; set; } = "auto";

    public void CancelCurrentRequest()
    {
        Func<Task>? recovery = null;
        bool hadActiveRequest = false;
        lock (_cancelLock)
        {
            if (_activeRequest != null)
            {
                hadActiveRequest = true;
                try { _activeRequest.Cancel(); } catch { }
            }
            if (hadActiveRequest && _cancelRecovery.IsCompleted)
            {
                recovery = CancellationRecoveryHandler;
                if (recovery != null) _cancelRecovery = RunCancellationRecoveryAsync(recovery);
            }
        }
    }

    async Task RunCancellationRecoveryAsync(Func<Task> recovery)
    {
        try { await recovery(); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("The AI job was cancelled, but the local AI service could not be reset cleanly: " + ex.Message, ex);
        }
    }

    async Task WaitForCancellationRecoveryAsync()
    {
        Task recovery;
        lock (_cancelLock) recovery = _cancelRecovery;
        await recovery;
    }

    public string LastDiagnostic { get; private set; } = "";

    public async Task<bool> HealthAsync()
    {
        try
        {
            await WaitForCancellationRecoveryAsync();
            bool ok = await ProbeHealthAsync();
            if (!ok) LastDiagnostic = "The local AI backend did not answer its health check.";
            return ok;
        }
        catch (Exception ex)
        {
            LastDiagnostic = ex.Message;
            return false;
        }
    }

    internal async Task<bool> ProbeHealthAsync()
    {
        try { using var r = await _http.GetAsync($"{BackendUrl}/health"); return r.IsSuccessStatusCode; }
        catch { return false; }
    }

    public async Task<string> GenerateConceptAsync(string prompt, string outputPath, string quality = "standard")
        => await PostForFileAsync("/generate-concept", new { prompt, output_path = outputPath, quality, provider = ImageGenerateProvider });
    public async Task<string> EditImageAsync(string imagePath, string? maskPath, string prompt, string outputPath, string quality = "standard")
        => await PostForFileAsync("/edit-image", new { image_path = imagePath, mask_path = maskPath, prompt, output_path = outputPath, quality, provider = ImageEditProvider });
    public async Task<string> Generate3DAsync(string imagePath, string prompt, string outputPath, string quality = "standard")
        => await PostForFileAsync("/generate-3d", new { image_path = imagePath, prompt, output_path = outputPath, quality, provider = Quality3DProvider, role = "quality" });
    public async Task<string> Generate3DRoutedAsync(string imagePath, string prompt, string outputPath, string role, string provider = "auto")
        => await PostForFileAsync("/generate-3d", new { image_path = imagePath, prompt, output_path = outputPath, quality = "standard", provider, role });
    public async Task<string> GeneratePartsAsync(string imagePath, string outputDir, int numParts, string tag = "miniscuplter", string provider = "auto")
        => await PostJsonTextAsync("/generate-parts", new { image_path = imagePath, output_dir = outputDir, num_parts = numParts, tag, provider }, true);
    public async Task<string> Detail2DAsync(string imagePath, string maskPath, string prompt, string outputPath)
        => await PostForFileAsync("/detail-2d", new { image_path = imagePath, mask_path = maskPath, prompt, output_path = outputPath, image_provider = ImageDetailProvider });
    public async Task<string> Detail3DAsync(string sourceMesh, string imagePath, string maskPath, string prompt, float[] boundsMin, float[] boundsMax,
        string outputPatch, string outputImage, string outputCrop)
        => await PostJsonTextAsync("/detail-3d", new { source_mesh = sourceMesh, image_path = imagePath, mask_path = maskPath, prompt,
            bounds_min = boundsMin, bounds_max = boundsMax, output_patch = outputPatch, output_image = outputImage, output_crop = outputCrop,
            image_provider = ImageDetailProvider, three_d_provider = Detail3DProvider }, true);
    public async Task<string> ApplyDetailAsync(string sourceMesh, string patchMesh, string outputPath, double? voxelSize = null)
        => await PostForFileAsync("/detail-apply", new { source_mesh = sourceMesh, patch_mesh = patchMesh, output_path = outputPath, voxel_size = voxelSize });
    public async Task<string> GetRoutingAsync()
    {
        await WaitForCancellationRecoveryAsync();
        using var response = await _http.GetAsync(BackendUrl + "/routing"); var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(body); return body;
    }

    public async Task<string> VoxelRemeshAsync(IReadOnlyList<string> inputPaths, string outputPath, double voxelSize) => await PostForFileAsync("/geometry/voxel-remesh", new { input_paths = inputPaths, output_path = outputPath, voxel_size = voxelSize });
    public async Task<string> AnalyzeGeometryAsync(string inputPath, double featureThresholdMm) => await PostJsonTextAsync("/geometry/analyze", new { input_path = inputPath, feature_threshold_mm = featureThresholdMm });
    public async Task<string> ThicknessMapAsync(string inputPath, double targetMm, int maxSamples = 12000) => await PostJsonTextAsync("/geometry/thickness-map", new { input_path = inputPath, target_mm = targetMm, max_samples = maxSamples }, true);
    public async Task<string> RepairGeometryAsync(string inputPath, string outputPath, double voxelSize) => await PostForFileAsync("/geometry/repair", new { input_path = inputPath, output_path = outputPath, voxel_size = voxelSize });
    public async Task<string> PredictRigAsync(string inputPath, string outputPath, string mode = "quick", int seed = 0, double branchThreshold = 0.28) => await PostForFileAsync("/rig/predict-skeleton", new { input_path = inputPath, output_path = outputPath, mode, seed, branch_threshold = branchThreshold });
    public async Task<string> SemanticSelectAsync(string inputPath, string query) => await PostJsonTextAsync("/semantic-select", new { input_path = inputPath, query }, true);

    public async Task ApplyQualityConfigAsync(int imageSize, int imageSteps, double imageGuidance, double imageEditStrength, int maxInputPx, int shapeSteps,
        double remeshVoxelMm, double repairVoxelMm, long maxVoxelCells, int thicknessSamples, int smartSelectViews, int smartSelectRenderSize)
    {
        await PostJsonTextAsync("/geometry/quality-config", new {
            image_size=imageSize, image_steps=imageSteps, image_guidance=imageGuidance, image_edit_strength=imageEditStrength, max_input_px=maxInputPx,
            shape_steps=shapeSteps, remesh_voxel_mm=remeshVoxelMm, repair_voxel_mm=repairVoxelMm, max_voxel_cells=maxVoxelCells,
            thickness_samples=thicknessSamples, smart_select_views=smartSelectViews, smart_select_render_size=smartSelectRenderSize
        }, true);
    }

    async Task<string> PostForFileAsync(string route, object payload)
    {
        var body = await PostJsonTextAsync(route, payload, true);
        using var doc = JsonDocument.Parse(body);
        string path = doc.RootElement.GetProperty("path").GetString() ?? throw new InvalidOperationException("Backend returned no file path.");
        if (!File.Exists(path)) throw new InvalidOperationException($"Backend reported success but output file does not exist: {path}");
        if (new FileInfo(path).Length == 0) throw new InvalidOperationException($"Backend reported success but output file is empty: {path}");
        return path;
    }

    async Task<string> PostJsonTextAsync(string route, object payload, bool cancellable = false)
    {
        await WaitForCancellationRecoveryAsync();
        CancellationTokenSource? cts = cancellable ? new CancellationTokenSource() : null;
        bool gateHeld = false;
        string? jobId = cancellable ? Guid.NewGuid().ToString("N") : null;
        try
        {
            if (cancellable)
            {
                await _jobGate.WaitAsync(cts!.Token);
                gateHeld = true;
                lock (_cancelLock)
                {
                    _activeRequest?.Cancel();
                    _activeRequest?.Dispose();
                    _activeRequest = cts;
                    _activeJobId = jobId;
                }
            }

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, BackendUrl + route)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrWhiteSpace(jobId))
                request.Headers.TryAddWithoutValidation("X-Miniscupter-Job-Id", jobId);

            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                cts?.Token ?? CancellationToken.None);
            var body = await response.Content.ReadAsStringAsync(cts?.Token ?? CancellationToken.None);

            if (response.Headers.TryGetValues("X-Miniscupter-Job-Id", out var responseIds))
            {
                string? returned = responseIds.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(returned))
                    jobId = returned;
            }

            if (!response.IsSuccessStatusCode)
            {
                string detail = string.IsNullOrWhiteSpace(body)
                    ? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                    : body;
                throw new InvalidOperationException(detail);
            }
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidOperationException("Backend returned an empty response.");
            return body;
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException("Operation cancelled by user.");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Backend connection failed: " + ex.Message, ex);
        }
        finally
        {
            if (cts != null)
            {
                lock (_cancelLock)
                {
                    if (ReferenceEquals(_activeRequest, cts))
                    {
                        _activeRequest = null;
                        _activeJobId = null;
                    }
                    _lastJobId = jobId;
                }
                cts.Dispose();
            }
            if (gateHeld) _jobGate.Release();
        }
    }

    public async Task<AiJobProgress> GetJobProgressAsync(string jobId)
    {
        await WaitForCancellationRecoveryAsync();
        using var response = await _http.GetAsync($"{BackendUrl}/job-progress/{Uri.EscapeDataString(jobId)}");
        string body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? $"Job progress HTTP {(int)response.StatusCode}." : body);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        string GetString(string name, string fallback = "") =>
            root.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
                ? value.GetString() ?? fallback
                : fallback;
        double GetDouble(string name) =>
            root.TryGetProperty(name, out var value) && value.TryGetDouble(out var number) ? number : 0;
        bool GetBool(string name) =>
            root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
        long GetLong(string name) =>
            root.TryGetProperty(name, out var value) && value.TryGetInt64(out var number) ? number : 0;
        return new AiJobProgress(
            GetString("job_id", jobId),
            GetString("kind"),
            GetString("state", "running"),
            GetString("stage", "working"),
            GetString("detail"),
            GetDouble("progress"),
            GetString("provider"),
            GetBool("active"),
            GetBool("cancel_requested"),
            GetLong("sequence"));
    }

    public async Task<AiComponentStatus> GetComponentsAsync()
    {
        await WaitForCancellationRecoveryAsync();
        using var response = await _http.GetAsync(BackendUrl + "/components"); var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(body);
        using var doc = JsonDocument.Parse(body); var root = doc.RootElement; var hw = root.GetProperty("hardware");
        var hardware = new AiHardwareInfo(hw.TryGetProperty("gpu", out var gpu) && gpu.ValueKind != JsonValueKind.Null ? gpu.GetString() : null, hw.TryGetProperty("vram_mb", out var vm) ? vm.GetInt32() : 0, hw.TryGetProperty("cuda_available", out var ca) && ca.GetBoolean(), hw.TryGetProperty("recommended_profile", out var rp) ? rp.GetString() ?? "unknown" : "unknown");
        var items = new List<AiComponentInfo>();
        foreach (var e in root.GetProperty("components").EnumerateArray()) items.Add(new AiComponentInfo(e.GetProperty("id").GetString() ?? "unknown", e.GetProperty("name").GetString() ?? "AI Component", e.GetProperty("kind").GetString() ?? "unknown", e.GetProperty("installed").GetBoolean(), e.TryGetProperty("estimated_gb", out var gb) ? gb.GetDouble() : 0, e.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "", e.TryGetProperty("path", out var p) && p.ValueKind != JsonValueKind.Null ? p.GetString() : null));
        string dataRoot = root.TryGetProperty("data_root", out var dr) ? dr.GetString() ?? "" : ""; return new AiComponentStatus(hardware, items, dataRoot);
    }

    public async Task InstallComponentAsync(string id) => await PostJsonAsync("/components/install", new { id });
    public async Task UninstallComponentAsync(string id) => await PostJsonAsync("/components/uninstall", new { id });
    public async Task ReleaseModelsAsync() => await PostJsonAsync("/release-models", new { });
    async Task PostJsonAsync(string route, object payload)
    {
        await PostJsonTextAsync(route, payload, true);
    }

    public async Task<List<ReferenceResult>> SearchReferencesAsync(string query, int limit = 8)
    {
        if (!InternetReferencesEnabled) throw new InvalidOperationException("Internet reference access is disabled in Settings.");
        var url = "https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrsearch=" + Uri.EscapeDataString(query) + $"&gsrnamespace=6&gsrlimit={Math.Clamp(limit,1,20)}&prop=imageinfo&iiprop=url&iiurlwidth=320&format=json&origin=*";
        var text = await _http.GetStringAsync(url); using var doc = JsonDocument.Parse(text); var results = new List<ReferenceResult>();
        if (!doc.RootElement.TryGetProperty("query", out var q) || !q.TryGetProperty("pages", out var pages)) return results;
        foreach (var page in pages.EnumerateObject()) { var e = page.Value; string title = e.TryGetProperty("title", out var t) ? t.GetString() ?? "Reference" : "Reference"; string pageUrl = "https://commons.wikimedia.org/wiki/" + Uri.EscapeDataString(title.Replace(' ', '_')); string? thumb = null; if (e.TryGetProperty("imageinfo", out var ii) && ii.GetArrayLength() > 0) { var info = ii[0]; if (info.TryGetProperty("thumburl", out var tu)) thumb = tu.GetString(); else if (info.TryGetProperty("url", out var u)) thumb = u.GetString(); } results.Add(new ReferenceResult(title, pageUrl, thumb)); }
        return results;
    }
}
