using Godot;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public record ReferenceResult(string Title, string PageUrl, string? ThumbnailUrl);

public sealed class AIClient
{
    readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(30) };
    public string BackendUrl { get; set; } = "http://127.0.0.1:7868";
    public bool InternetReferencesEnabled { get; set; } = true;

    public async Task<bool> HealthAsync()
    {
        try { using var r = await _http.GetAsync($"{BackendUrl}/health"); return r.IsSuccessStatusCode; }
        catch { return false; }
    }

    public async Task<string> GenerateConceptAsync(string prompt, string outputPath)
        => await PostForFileAsync("/generate-concept", new { prompt, output_path = outputPath });

    public async Task<string> EditImageAsync(string imagePath, string? maskPath, string prompt, string outputPath)
        => await PostForFileAsync("/edit-image", new { image_path = imagePath, mask_path = maskPath, prompt, output_path = outputPath });

    public async Task<string> Generate3DAsync(string imagePath, string prompt, string outputPath)
        => await PostForFileAsync("/generate-3d", new { image_path = imagePath, prompt, output_path = outputPath });

    async Task<string> PostForFileAsync(string route, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        using var response = await _http.PostAsync(BackendUrl + route, new StringContent(json, Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(body);
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("path").GetString() ?? throw new InvalidOperationException("AI backend returned no file path.");
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
