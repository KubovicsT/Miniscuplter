using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    sealed record V1016Reference(
        string Provider,
        string Source,
        string Title,
        string Creator,
        string License,
        string PageUrl,
        string ImageUrl,
        string ThumbnailUrl);

    sealed record V1016ProviderResult(string Name, List<V1016Reference> Items, string? Error);

    static readonly HttpClient V1016ReferenceHttp = CreateV1016ReferenceHttp();

    LineEdit? _v1016ReferenceQuery;
    OptionButton? _v1016ReferenceSource;
    Button? _v1016ReferenceSearchButton;
    Label? _v1016ReferenceStatus;
    VBoxContainer? _v1016ReferenceResults;

    static HttpClient CreateV1016ReferenceHttp()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Miniscuplter/1.0.16 (+https://github.com/KubovicsT/Miniscuplter)");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json, image/*;q=0.9, */*;q=0.5");
        return client;
    }

    public void InstallV1016ReferenceSearch()
    {
        if (FindChild("2D", true, false) is not VBoxContainer twoD) return;
        if (FindChild("Reference Image Browser v1.0.16", true, false) != null) return;

        // v1.0.15 made reference results visible/selectable, but its provider was intentionally
        // limited to Commons. Keep that proven implementation intact and replace only its visible
        // surface so rollback remains trivial.
        if (FindChild("Reference Image Browser", true, false) is Control oldBrowser)
            oldBrowser.Visible = false;

        var section = new VBoxContainer { Name = "Reference Image Browser v1.0.16" };
        section.AddChild(Heading("REFERENCE IMAGES"));
        section.AddChild(new Label
        {
            Text = "Search multiple public image sources. All Sources combines Openverse (which aggregates sources such as Flickr, museums and other open collections) with Wikimedia Commons. Results show their origin and license when available before you choose one as the 2D source.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        _v1016ReferenceQuery = new LineEdit
        {
            PlaceholderText = "Reference search terms (leave empty to use Prompt)",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        section.AddChild(_v1016ReferenceQuery);

        var sourceRow = new HBoxContainer();
        sourceRow.AddChild(new Label { Text = "Source" });
        _v1016ReferenceSource = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _v1016ReferenceSource.AddItem("All Sources (Openverse + Wikimedia)");
        _v1016ReferenceSource.AddItem("Openverse — multi-source open images");
        _v1016ReferenceSource.AddItem("Wikimedia Commons");
        sourceRow.AddChild(_v1016ReferenceSource);
        section.AddChild(sourceRow);

        _v1016ReferenceSearchButton = new Button { Text = "Search Reference Images" };
        _v1016ReferenceSearchButton.Pressed += async () => await SearchV1016ReferencesAsync();
        section.AddChild(_v1016ReferenceSearchButton);

        _v1016ReferenceStatus = new Label
        {
            Text = "Reference search: ready · All Sources uses Openverse + Wikimedia Commons",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        section.AddChild(_v1016ReferenceStatus);

        _v1016ReferenceResults = new VBoxContainer { Name = "Multi-source Reference Results" };
        section.AddChild(_v1016ReferenceResults);
        section.AddChild(new HSeparator());

        twoD.AddChild(section);
    }

    async Task SearchV1016ReferencesAsync()
    {
        if (_v1016ReferenceResults == null || _v1016ReferenceStatus == null || _v1016ReferenceSearchButton == null) return;

        string query = _v1016ReferenceQuery?.Text.Trim() ?? "";
        if (query.Length == 0) query = _prompt?.Text.Trim() ?? "";
        if (query.Length == 0)
        {
            _v1016ReferenceStatus.Text = "Reference search: enter search terms above or in Prompt first.";
            return;
        }
        if (_internetToggle != null && !_internetToggle.ButtonPressed)
        {
            _v1016ReferenceStatus.Text = "Reference search: internet access is disabled.";
            return;
        }

        foreach (Node child in _v1016ReferenceResults.GetChildren()) child.QueueFree();
        _v1016ReferenceSearchButton.Disabled = true;

        int mode = _v1016ReferenceSource?.Selected ?? 0;
        string modeLabel = mode switch
        {
            1 => "Openverse",
            2 => "Wikimedia Commons",
            _ => "Openverse + Wikimedia Commons"
        };
        _v1016ReferenceStatus.Text = $"Reference search: searching {modeLabel}…";

        try
        {
            var jobs = new List<Task<V1016ProviderResult>>();
            if (mode is 0 or 1) jobs.Add(RunV1016ProviderAsync("Openverse", () => SearchV1016OpenverseAsync(query)));
            if (mode is 0 or 2) jobs.Add(RunV1016ProviderAsync("Wikimedia Commons", () => SearchV1016WikimediaAsync(query)));

            V1016ProviderResult[] batches = await Task.WhenAll(jobs);
            var dedup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var merged = new List<V1016Reference>();
            foreach (var batch in batches)
            {
                foreach (var item in batch.Items)
                {
                    string key = string.IsNullOrWhiteSpace(item.PageUrl) ? item.ImageUrl : item.PageUrl;
                    if (dedup.Add(key)) merged.Add(item);
                }
            }

            // Openverse already spans many upstream collections; keeping the visible set bounded
            // prevents a single search from starting dozens of thumbnail downloads at once.
            merged = merged.Take(14).ToList();
            foreach (var item in merged) AddV1016ReferenceCard(item);

            string counts = string.Join(" · ", batches.Select(b => $"{b.Name}: {b.Items.Count}"));
            string failures = string.Join(" | ", batches.Where(b => !string.IsNullOrWhiteSpace(b.Error)).Select(b => $"{b.Name} failed: {b.Error}"));
            if (merged.Count == 0)
                _v1016ReferenceStatus.Text = failures.Length == 0
                    ? $"Reference search: no usable images found. {counts}"
                    : $"Reference search: no usable images found. {failures}";
            else
                _v1016ReferenceStatus.Text = $"Reference search: {merged.Count} visible result(s) · {counts}"
                    + (failures.Length == 0 ? "" : $" · Partial failure: {failures}");
        }
        catch (Exception ex)
        {
            _v1016ReferenceStatus.Text = "Reference search FAILED: " + ex.Message;
            V109ShowError("Internet reference search failed", ex.Message);
        }
        finally
        {
            _v1016ReferenceSearchButton.Disabled = false;
        }
    }

    async Task<V1016ProviderResult> RunV1016ProviderAsync(string name, Func<Task<List<V1016Reference>>> search)
    {
        try
        {
            return new V1016ProviderResult(name, await search(), null);
        }
        catch (Exception ex)
        {
            // A multi-source search should degrade rather than fail because one public service is
            // unavailable or rate-limited. The failure remains visible in the status text.
            return new V1016ProviderResult(name, new List<V1016Reference>(), ex.Message);
        }
    }

    async Task<List<V1016Reference>> SearchV1016OpenverseAsync(string query)
    {
        string url = "https://api.openverse.org/v1/images/?q=" + Uri.EscapeDataString(query)
            + "&page_size=10&mature=false";
        using var response = await V1016ReferenceHttp.GetAsync(url);
        string body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");

        using var doc = JsonDocument.Parse(body);
        var results = new List<V1016Reference>();
        if (!doc.RootElement.TryGetProperty("results", out var array) || array.ValueKind != JsonValueKind.Array)
            return results;

        foreach (var item in array.EnumerateArray())
        {
            string original = V1016JsonString(item, "url");
            string thumb = V1016JsonString(item, "thumbnail");
            if (!V1016HttpUrl(original) || !V1016HttpUrl(thumb)) continue;
            if (item.TryGetProperty("watermarked", out var wm) && wm.ValueKind == JsonValueKind.True) continue;

            string title = V1016JsonString(item, "title");
            if (string.IsNullOrWhiteSpace(title)) title = "Untitled reference";
            string page = V1016JsonString(item, "foreign_landing_url");
            if (!V1016HttpUrl(page)) page = original;
            string source = V1016JsonString(item, "source");
            string provider = V1016JsonString(item, "provider");
            string creator = V1016JsonString(item, "creator");
            string license = V1016OpenverseLicense(item);

            string origin = V1016PrettySource(source);
            string providerPretty = V1016PrettySource(provider);
            if (origin.Length == 0) origin = providerPretty;
            else if (providerPretty.Length > 0 && !origin.Equals(providerPretty, StringComparison.OrdinalIgnoreCase))
                origin += $" via {providerPretty}";
            if (origin.Length == 0) origin = "Openverse collection";

            results.Add(new V1016Reference("Openverse", origin, title, creator, license, page, original, thumb));
        }
        return results;
    }

    async Task<List<V1016Reference>> SearchV1016WikimediaAsync(string query)
    {
        string url = "https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrsearch=" + Uri.EscapeDataString(query)
            + "&gsrnamespace=6&gsrlimit=8&prop=imageinfo&iiprop=url%7Cmime%7Cextmetadata&iiurlwidth=1024&format=json";
        using var response = await V1016ReferenceHttp.GetAsync(url);
        string body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");

        using var doc = JsonDocument.Parse(body);
        var results = new List<V1016Reference>();
        if (!doc.RootElement.TryGetProperty("query", out var q) || !q.TryGetProperty("pages", out var pages))
            return results;

        foreach (var pageProp in pages.EnumerateObject())
        {
            JsonElement page = pageProp.Value;
            string title = V1016JsonString(page, "title");
            if (!page.TryGetProperty("imageinfo", out var infos) || infos.ValueKind != JsonValueKind.Array || infos.GetArrayLength() == 0) continue;
            JsonElement info = infos[0];
            string mime = V1016JsonString(info, "mime");
            if (!mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) continue;
            string original = V1016JsonString(info, "url");
            string thumb = V1016JsonString(info, "thumburl");
            string landing = V1016JsonString(info, "descriptionurl");
            if (!V1016HttpUrl(original)) continue;
            if (!V1016HttpUrl(thumb)) thumb = original;
            if (!V1016HttpUrl(landing)) landing = original;

            string license = "";
            string creator = "";
            if (info.TryGetProperty("extmetadata", out var meta) && meta.ValueKind == JsonValueKind.Object)
            {
                license = V1016MetadataValue(meta, "LicenseShortName");
                creator = V1016StripTags(V1016MetadataValue(meta, "Artist"));
            }
            results.Add(new V1016Reference("Wikimedia Commons", "Wikimedia Commons", title, creator, license, landing, original, thumb));
        }
        return results;
    }

    void AddV1016ReferenceCard(V1016Reference item)
    {
        if (_v1016ReferenceResults == null) return;
        var card = new VBoxContainer();
        card.AddChild(new Label
        {
            Text = item.Title,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        string attribution = $"Source: {item.Source}";
        if (!string.IsNullOrWhiteSpace(item.Creator)) attribution += $" · Creator: {item.Creator}";
        if (!string.IsNullOrWhiteSpace(item.License)) attribution += $" · License: {item.License}";
        card.AddChild(new Label
        {
            Text = attribution,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            TooltipText = $"Provider: {item.Provider}\nOpen the source page for full attribution and usage terms."
        });

        var preview = new TextureRect
        {
            CustomMinimumSize = new Vector2(0, 150),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        card.AddChild(preview);

        var row = new HBoxContainer();
        var use = new Button { Text = "Use as 2D Source", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        use.Pressed += async () => await UseV1016ReferenceAsync(item);
        var open = new Button { Text = "Open Source" };
        open.Pressed += () => OS.ShellOpen(item.PageUrl);
        row.AddChild(use);
        row.AddChild(open);
        card.AddChild(row);
        card.AddChild(new HSeparator());
        _v1016ReferenceResults.AddChild(card);

        _ = LoadV1016ThumbnailAsync(preview, item);
    }

    async Task LoadV1016ThumbnailAsync(TextureRect preview, V1016Reference item)
    {
        try
        {
            string cached = await DownloadV1016ReferenceAsync(item, thumbnail: true);
            if (!GodotObject.IsInstanceValid(preview)) return;
            var image = Image.LoadFromFile(cached);
            if (image != null && !image.IsEmpty()) preview.Texture = ImageTexture.CreateFromImage(image);
        }
        catch (Exception ex)
        {
            if (GodotObject.IsInstanceValid(preview)) preview.TooltipText = "Thumbnail could not be loaded: " + ex.Message;
        }
    }

    async Task UseV1016ReferenceAsync(V1016Reference item)
    {
        try
        {
            if (_v1016ReferenceStatus != null) _v1016ReferenceStatus.Text = $"Downloading selected {item.Source} reference…";
            string cached = await DownloadV1016ReferenceAsync(item, thumbnail: false);
            SetStartingImage(cached);
            string current = CurrentV1015ImageSource();
            if (!string.IsNullOrWhiteSpace(current)) SyncV1015CanvasSource(current);
            if (_v1016ReferenceStatus != null)
                _v1016ReferenceStatus.Text = $"Selected {item.Source} image is now the current 2D source. Review/edit it, then accept it as baseline when ready.";
        }
        catch (Exception ex)
        {
            if (_v1016ReferenceStatus != null) _v1016ReferenceStatus.Text = "Could not use reference: " + ex.Message;
            V109ShowError("Reference image", ex.Message);
        }
    }

    async Task<string> DownloadV1016ReferenceAsync(V1016Reference item, bool thumbnail)
    {
        string[] candidates = thumbnail
            ? new[] { item.ThumbnailUrl, item.ImageUrl }
            : new[] { item.ImageUrl, item.ThumbnailUrl };
        Exception? last = null;
        foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!V1016HttpUrl(candidate)) continue;
            try
            {
                return await DownloadV1016ImageUrlAsync(candidate, thumbnail ? "thumb" : "source", thumbnail ? 8L * 1024 * 1024 : 40L * 1024 * 1024);
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }
        throw new InvalidOperationException(last == null ? "Reference result has no usable downloadable image URL." : last.Message, last);
    }

    async Task<string> DownloadV1016ImageUrlAsync(string url, string prefix, long maxBytes)
    {
        using var response = await V1016ReferenceHttp.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is long length && length > maxBytes)
            throw new InvalidDataException($"Reference image is too large ({length / (1024 * 1024)} MB; limit {maxBytes / (1024 * 1024)} MB). Open the source page and choose a smaller image.");

        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        if (bytes.Length == 0) throw new InvalidDataException("Reference server returned an empty image.");
        if (bytes.LongLength > maxBytes) throw new InvalidDataException("Reference image exceeded the download size limit.");

        string media = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
        string ext = media.Contains("png") ? ".png"
            : media.Contains("webp") ? ".webp"
            : media.Contains("jpeg") || media.Contains("jpg") ? ".jpg"
            : V1016ExtensionFromUrl(url);
        if (ext is not ".png" and not ".webp" and not ".jpg" and not ".jpeg") ext = ".jpg";

        string dir = ProjectSettings.GlobalizePath("user://reference_cache");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"{prefix}_{Guid.NewGuid():N}{ext}");
        await File.WriteAllBytesAsync(path, bytes);

        var decoded = Image.LoadFromFile(path);
        if (decoded == null || decoded.IsEmpty())
        {
            try { File.Delete(path); } catch { }
            throw new InvalidDataException("Downloaded reference is not a supported raster image. Miniscuplter will try the preview image instead when available.");
        }
        return path;
    }

    static string V1016JsonString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
    }

    static bool V1016HttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    static string V1016PrettySource(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return string.Join(" ", value.Replace('-', ' ').Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }

    static string V1016OpenverseLicense(JsonElement item)
    {
        string code = V1016JsonString(item, "license").ToLowerInvariant();
        string version = V1016JsonString(item, "license_version");
        string name = code switch
        {
            "cc0" => "CC0",
            "pdm" => "Public Domain Mark",
            "by" => "CC BY",
            "by-sa" => "CC BY-SA",
            "by-nd" => "CC BY-ND",
            "by-nc" => "CC BY-NC",
            "by-nc-sa" => "CC BY-NC-SA",
            "by-nc-nd" => "CC BY-NC-ND",
            _ => code.ToUpperInvariant()
        };
        if (name.Length > 0 && version.Length > 0 && code is not "cc0" and not "pdm") name += " " + version;
        return name;
    }

    static string V1016MetadataValue(JsonElement metadata, string name)
    {
        if (!metadata.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.Object) return "";
        return V1016JsonString(item, "value");
    }

    static string V1016StripTags(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        // Commons artist metadata is often a tiny HTML fragment. Avoid adding an HTML parser just
        // for a label; preserve useful text and keep full attribution one click away on the source.
        var chars = new List<char>(value.Length);
        bool inside = false;
        foreach (char c in value)
        {
            if (c == '<') { inside = true; continue; }
            if (c == '>') { inside = false; chars.Add(' '); continue; }
            if (!inside) chars.Add(c);
        }
        string cleaned = new string(chars.ToArray()).Replace("&nbsp;", " ").Replace("&amp;", "&").Trim();
        return cleaned.Length > 180 ? cleaned[..180] + "…" : cleaned;
    }

    static string V1016ExtensionFromUrl(string url)
    {
        try
        {
            string ext = Path.GetExtension(new Uri(url).AbsolutePath).ToLowerInvariant();
            return ext;
        }
        catch
        {
            return "";
        }
    }
}
