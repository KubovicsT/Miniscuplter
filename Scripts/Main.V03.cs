using Godot;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    Label? _v03SourceStatus;
    string _v03StartingImage = "";

    public void InstallV03Extras()
    {
        var aiPanel = FindChild("AI", true, false) as VBoxContainer;
        if (aiPanel == null) return;

        aiPanel.AddChild(new HSeparator());
        aiPanel.AddChild(new Label { Text = "Starting Image — v0.3", ThemeTypeVariation = "HeaderSmall" });
        aiPanel.AddChild(new Label
        {
            Text = "Use AI to generate a concept, or load your own artwork/reference image and continue from there.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        var row = new HBoxContainer();
        var load = new Button { Text = "Use My Image", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var clear = new Button { Text = "Clear" };
        load.Pressed += OpenStartingImageDialog;
        clear.Pressed += ClearStartingImage;
        row.AddChild(load);
        row.AddChild(clear);
        aiPanel.AddChild(row);

        var edit = new Button { Text = "Edit Starting Image With AI" };
        edit.Pressed += async () => await EditStartingImageWithAi();
        aiPanel.AddChild(edit);

        _v03SourceStatus = new Label
        {
            Text = "Starting source: AI-generated concept or viewport capture",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        aiPanel.AddChild(_v03SourceStatus);
    }

    void OpenStartingImageDialog()
    {
        var d = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[]
            {
                "*.png ; PNG images",
                "*.jpg,*.jpeg ; JPEG images",
                "*.webp ; WebP images",
                "*.bmp ; BMP images"
            },
            UseNativeDialog = true
        };
        AddChild(d);
        d.FileSelected += path =>
        {
            try { SetStartingImage(path); }
            finally { d.QueueFree(); }
        };
        d.Canceled += d.QueueFree;
        d.PopupCenteredRatio(.75f);
    }

    void SetStartingImage(string sourcePath)
    {
        try
        {
            if (!File.Exists(sourcePath)) throw new FileNotFoundException("Image file was not found.", sourcePath);
            var image = Image.LoadFromFile(sourcePath);
            if (image == null || image.IsEmpty()) throw new InvalidDataException("The selected file could not be decoded as an image.");

            string dir = ProjectSettings.GlobalizePath("user://source_images");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, $"source_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            var err = image.SavePng(dest);
            if (err != Error.Ok) throw new IOException("Could not store the selected image in the project workspace.");

            _v03StartingImage = dest;
            _lastEditedImage = dest;
            ShowAiPreview(dest);
            if (_v03SourceStatus != null)
                _v03SourceStatus.Text = "Starting source: user image · " + Path.GetFileName(sourcePath);
            SetStatus("User image loaded. You can edit it in 2D or generate a 3D part directly.");
        }
        catch (Exception ex)
        {
            SetStatus("Could not load starting image: " + ex.Message);
        }
    }

    async Task EditStartingImageWithAi()
    {
        string source = !string.IsNullOrEmpty(_v03StartingImage) ? _v03StartingImage : _lastEditedImage;
        if (string.IsNullOrEmpty(source) || !File.Exists(source))
        {
            SetStatus("Load your own image or generate a concept first.");
            return;
        }

        string prompt = _prompt?.Text.Trim() ?? "";
        if (prompt.Length == 0)
        {
            SetStatus("Describe how you want the starting image changed.");
            return;
        }

        string outPath = ProjectSettings.GlobalizePath($"user://source_edit_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        await RunAi(async () =>
        {
            _lastEditedImage = await _ai.EditImageAsync(source, null, prompt, outPath);
            ShowAiPreview(_lastEditedImage);
            if (_v03SourceStatus != null)
                _v03SourceStatus.Text = "Starting source: user image · AI-edited";
            SetStatus("Starting image edited. Review it, regenerate if needed, or generate 3D from the approved result.");
        });
    }

    void ClearStartingImage()
    {
        _v03StartingImage = "";
        if (_v03SourceStatus != null)
            _v03SourceStatus.Text = "Starting source: AI-generated concept or viewport capture";
        SetStatus("User starting image cleared. AI-generated concept/capture workflow remains active.");
    }
}
