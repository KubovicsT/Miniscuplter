using Godot;
using System;
using System.Collections.Generic;
using System.IO;
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

public sealed class AIClient
{
    readonly NetHttpClient _http = new() { Timeout = TimeSpan.FromMinutes(90) };
    readonly object _cancelLock = new();
    readonly SemaphoreSlim _jobGate = new(1, 1);
    CancellationTokenSource? _activeRequest;
    public string BackendUrl { get; set; } = "http://127.0.0.1:7868";
    public bool InternetReferencesEnabled { get; set; } = true;

    public void CancelCurrentRequest() { lock (_cancelLock) _activeRequest?.Cancel(); }
    public async Task<bool> HealthAsync() { try { using var r = await _http.GetAsync($"{BackendUrl}/health"); return r.IsSuccessStatusCode; } catch { return false; } }

    public async Task<string> GenerateConceptAsync(string prompt, string outputPath, string quality = "standard") => await PostForFileAsync("/generate-concept", new { prompt, output_path = outputPath, quality });
    public async Task<string> EditImageAsync(string imagePath, string? maskPath, string prompt, string outputPath, string quality = "standard") => await PostForFileAsync("/edit-image", new { image_path = imagePath, mask_path = maskPath, prompt, output_path = outputPath, quality });
    public async Task<string> Generate3DAsync(string imagePath, string prompt, string outputPath, string quality = "standard") => await PostForFileAsync("/generate-3d", new { image_path = imagePath, prompt, output_path = outputPath, quality });
    public async Task<string> VoxelRemeshAsync(IReadOnlyList<string> inputPaths, string outputPath, double voxelSize) => await PostForFileAsync("/geometry/voxel-remesh", new { input_paths = inputPaths, output_path = outputPath, voxel_size = voxelSize });
    public async Task<string> AnalyzeGeometryAsync(string inputPath, double featureThresholdMm) => await PostJsonTextAsync("/geometry/analyze", new { input_path = inputPath, feature_threshold_mm = featureThresholdMm });
    public async Task<string> ThicknessMapAsync(string inputPath, double targetMm, int maxSamples = 12000) => await PostJsonTextAsync("/geometry/thickness-map", new { input_path = inputPath, target_mm = targetMm, max_samples = maxSamples }, true);
    public async Task<string> RepairGeometryAsync(string inputPath, string outputPath, double voxelSize) => await PostForFileAsync("/geometry/repair", new { input_path = inputPath, output_path = outputPath, voxel_size = voxelSize });
    public async Task<string> PredictRigAsync(string inputPath, string outputPath, string mode = "quick", int seed = 0, double branchThreshold = 0.28) => await PostForFileAsync("/rig/predict-skeleton", new { input_path = inputPath, output_path = outputPath, mode, seed, branch_threshold = branchThreshold });
    public async Task<string> SemanticSelectAsync(string inputPath, string query) => await PostJsonTextAsync("/semantic-select", new { input_path = inputPath, query }, true);

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
        CancellationTokenSource? cts = cancellable ? new CancellationTokenSource() : null;
        bool gateHeld = false;
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
                }
            }

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, BackendUrl + route) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts?.Token ?? CancellationToken.None);
            var body = await response.Content.ReadAsStringAsync(cts?.Token ?? CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                string detail = string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}" : body;
                throw new InvalidOperationException(detail);
            }
            if (string.IsNullOrWhiteSpace(body)) throw new InvalidOperationException("Backend returned an empty response.");
            return body;
        }
        catch (OperationCanceledException) { throw new InvalidOperationException("Operation cancelled by user."); }
        catch (HttpRequestException ex) { throw new InvalidOperationException("Backend connection failed: " + ex.Message, ex); }
        finally
        {
            if (cts != null)
            {
                lock (_cancelLock) { if (ReferenceEquals(_activeRequest, cts)) _activeRequest = null; }
                cts.Dispose();
            }
            if (gateHeld) _jobGate.Release();
        }
    }

    public async Task<AiComponentStatus> GetComponentsAsync()
    {
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
    async Task PostJsonAsync(string route, object payload) { using var response = await _http.PostAsync(BackendUrl + route, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")); var body = await response.Content.ReadAsStringAsync(); if (!response.IsSuccessStatusCode) throw new InvalidOperationException(body); }

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
