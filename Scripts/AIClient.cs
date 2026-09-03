using Godot;
using System;
using System.Collections.Generic;
using NetHttpClient = System.Net.Http.HttpClient;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public record ReferenceResult(string Title, string PageUrl, string? ThumbnailUrl);
public record AiComponentInfo(string Id, string Name, string Kind, bool Installed, double EstimatedGb, string Description, string? Path);
public record AiHardwareInfo(string? Gpu, int VramMb, bool CudaAvailable, string RecommendedProfile);
public record AiComponentStatus(AiHardwareInfo Hardware, List<AiComponentInfo> Components, string DataRoot);

public sealed class AIClient
{
    readonly NetHttpClient _http = new() { Timeout = TimeSpan.FromMinutes(90) };
    public string BackendUrl { get; set; } = "http://127.0.0.1:7868";
    public bool InternetReferencesEnabled { get; set; } = true;

    public async Task<bool> HealthAsync()
    {
        try { using var r = await _http.GetAsync($"{BackendUrl}/health"); return r.IsSuccessStatusCode; }
        catch { return false; }
    }

    public async Task<string> GenerateConceptAsync(string prompt, string outputPath, string quality = "standard")
        => await PostForFileAsync("/generate-concept", new { prompt, output_path = outputPath, quality });

    public async Task<string> EditImageAsync(string imagePath, string? maskPath, string prompt, string outputPath, string quality = "standard")
        => await PostForFileAsync("/edit-image", new { image_path = imagePath, mask_path = maskPath, prompt, output_path = outputPath, quality });

    public async Task<string> Generate3DAsync(string imagePath, string prompt, string outputPath, string quality = "standard")
        => await PostForFileAsync("/generate-3d", new { image_path = imagePath, prompt, output_path = outputPath, quality });

    public async Task<string> VoxelRemeshAsync(IReadOnlyList<string> inputPaths, string outputPath, double voxelSize)
        => await PostForFileAsync("/geometry/voxel-remesh", new { input_paths = inputPaths, output_path = outputPath, voxel_size = voxelSize });

    async Task<string> PostForFileAsync(string route, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        using var response = await _http.PostAsync(BackendUrl + route, new StringContent(json, Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(body);
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("path").GetString() ?? throw new InvalidOperationException("AI backend returned no file path.");
    }

    public async Task<AiComponentStatus> GetComponentsAsync()
    {
        using var response = await _http.GetAsync(BackendUrl + "/components");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(body);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var hw = root.GetProperty("hardware");
        var hardware = new AiHardwareInfo(
            hw.TryGetProperty("gpu", out var gpu) && gpu.ValueKind != JsonValueKind.Null ? gpu.GetString() : null,
            hw.TryGetProperty("vram_mb", out var vm) ? vm.GetInt32() : 0,
            hw.TryGetProperty("cuda_available", out var ca) && ca.GetBoolean(),
            hw.TryGetProperty("recommended_profile", out var rp) ? rp.GetString() ?? "unknown" : "unknown");
        var items = new List<AiComponentInfo>();
        foreach (var e in root.GetProperty("components").EnumerateArray())
        {
            items.Add(new AiComponentInfo(
                e.GetProperty("id").GetString() ?? "unknown",
                e.GetProperty("name").GetString() ?? "AI Component",
                e.GetProperty("kind").GetString() ?? "unknown",
                e.GetProperty("installed").GetBoolean(),
                e.TryGetProperty("estimated_gb", out var gb) ? gb.GetDouble() : 0,
                e.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                e.TryGetProperty("path", out var p) && p.ValueKind != JsonValueKind.Null ? p.GetString() : null));
        }
        string dataRoot = root.TryGetProperty("data_root", out var dr) ? dr.GetString() ?? "" : "";
        return new AiComponentStatus(hardware, items, dataRoot);
    }

    public async Task InstallComponentAsync(string id) => await PostJsonAsync("/components/install", new { id });
    public async Task UninstallComponentAsync(string id) => await PostJsonAsync("/components/uninstall", new { id });
    public async Task ReleaseModelsAsync() => await PostJsonAsync("/release-models", new { });

    async Task PostJsonAsync(string route, object payload)
    {
        using var response = await _http.PostAsync(BackendUrl + route,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(body);
    }

    public async Task<List<ReferenceResult>> SearchReferencesAsync(string query, int limit = 8)
    {
        if (!InternetReferencesEnabled) throw new InvalidOperationException("Internet reference access is disabled in Settings.");
        var url = "https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrsearch=" + Uri.EscapeDataString(query) +
                  $"&gsrnamespace=6&gsrlimit={Math.Clamp(limit,1,20)}&prop=imageinfo&iiprop=url&iiurlwidth=320&format=json&origin=*";
        var text = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(text);
        var results = new List<ReferenceResult>();
        if (!doc.RootElement.TryGetProperty("query", out var q) || !q.TryGetProperty("pages", out var pages)) return results;
        foreach (var page in pages.EnumerateObject())
        {
            var e = page.Value;
            string title = e.TryGetProperty("title", out var t) ? t.GetString() ?? "Reference" : "Reference";
            string pageUrl = "https://commons.wikimedia.org/wiki/" + Uri.EscapeDataString(title.Replace(' ', '_'));
            string? thumb = null;
            if (e.TryGetProperty("imageinfo", out var ii) && ii.GetArrayLength() > 0)
            {
                var info = ii[0];
                if (info.TryGetProperty("thumburl", out var tu)) thumb = tu.GetString();
                else if (info.TryGetProperty("url", out var u)) thumb = u.GetString();
            }
            results.Add(new ReferenceResult(title, pageUrl, thumb));
        }
        return results;
    }
}
