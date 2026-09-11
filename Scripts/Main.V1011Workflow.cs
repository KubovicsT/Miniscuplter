using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Miniscuplter;

public partial class Main
{
    readonly List<(Control Page, string Title)> _v1011AdvancedSettingsPages = new();
    string _v1011BaselineImage = "";
    Label? _v1011BaselineStatus;
    HBoxContainer? _v1011Toolbar;

    public void InstallV1011Workflow()
    {
        InstallV1011SettingsButton();
        RebuildV1011WorkflowTabs();
        EnforceV1011ApprovedBaseline();
        RepairV1011ViewportAtLaunch();
    }

    void InstallV1011SettingsButton()
    {
        var root = GetChildren().OfType<VBoxContainer>().FirstOrDefault();
        _v1011Toolbar = root?.GetChildren().OfType<HBoxContainer>().FirstOrDefault();
        if (_v1011Toolbar == null) return;

        var existing = _v1011Toolbar.GetChildren().OfType<Button>().FirstOrDefault(b => b.Text == "Settings");
        if (existing != null)
        {
            existing.Pressed += () => CallDeferred(nameof(AttachV1011AdvancedSettingsToWindow));
            return;
        }

        var settings = new Button { Text = "Settings", TooltipText = "AI models, quality, GPU performance, files and viewport options" };
        settings.Pressed += ShowV1011Settings;
        _v1011Toolbar.AddChild(settings);
    }

    void ShowV1011Settings()
    {
        ShowV109Settings();
        CallDeferred(nameof(AttachV1011AdvancedSettingsToWindow));
    }

    void AttachV1011AdvancedSettingsToWindow()
    {
        if (_v109SettingsWindow == null || !IsInstanceValid(_v109SettingsWindow)) return;
        _v109SettingsWindow.Title = "Miniscuplter Settings";
        if (_v109SettingsWindow.FindChild("TabContainer", true, false) is TabContainer tabs)
            AttachV1011AdvancedSettings(tabs);
    }

    void RebuildV1011WorkflowTabs()
    {
        var host = FindChild("ViewportHost", true, false) as SubViewportContainer;
        var split = host?.GetParent() as HSplitContainer;
        var tabs = split?.GetChildren().OfType<TabContainer>().FirstOrDefault();
        if (tabs == null) return;

        var oldPages = tabs.GetChildren().OfType<Control>().ToList();
        foreach (var page in oldPages) tabs.RemoveChild(page);

        var twoD = WorkflowPage("2D");
        var threeD = WorkflowPage("3D");
        var rig = WorkflowPage("Rig & Pose");
        var cleanup = WorkflowPage("Cleanup & Export");
        var runtimeAdvanced = WorkflowPage("AI Runtime & Components Content");
        _v1011AdvancedSettingsPages.Add((runtimeAdvanced, "AI Runtime & Components"));

        tabs.AddChild(twoD); tabs.AddChild(threeD); tabs.AddChild(rig); tabs.AddChild(cleanup);

        twoD.AddChild(Heading("1. 2D BASELINE"));
        twoD.AddChild(new Label { Text = "Generate a concept or load your own image, optionally edit it with AI, then explicitly accept the image you want to use as the 3D baseline.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var accept = new Button { Text = "Accept Current Image as Baseline" };
        accept.Pressed += AcceptV1011Baseline;
        twoD.AddChild(accept);
        _v1011BaselineStatus = new Label { Text = "Baseline: not accepted yet", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        twoD.AddChild(_v1011BaselineStatus);
        twoD.AddChild(new HSeparator());

        threeD.AddChild(Heading("2. 3D CREATE & EDIT"));
        threeD.AddChild(new Label { Text = "Generate the first mesh from the accepted 2D baseline, then sculpt, mask/refine parts, generate alternatives and kitbash non-destructively.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        threeD.AddChild(new HSeparator());

        rig.AddChild(Heading("3. RIG & POSE"));
        rig.AddChild(new Label { Text = "Create or refine a rig, edit joints, use IK and pose the model before final cleanup.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        rig.AddChild(new HSeparator());

        cleanup.AddChild(Heading("4. MESH CLEANUP & EXPORT"));
        cleanup.AddChild(new Label { Text = "Inspect, repair, remesh/finalize when needed, check thickness/structure and export the finished model.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        var export = new Button { Text = "Export Selected STL" }; export.Pressed += ExportStlDialog; cleanup.AddChild(export);
        cleanup.AddChild(new HSeparator());

        MoveV1011SculptControlsTo3D(threeD);

        foreach (var page in oldPages)
        {
            string name = page.Name.ToString();
            if (name.Contains("Quality", StringComparison.OrdinalIgnoreCase))
            {
                PrepareV1011AdvancedPage(page, "Advanced Quality");
                continue;
            }
            if (name.Contains("AI Models", StringComparison.OrdinalIgnoreCase))
            {
                PrepareV1011AdvancedPage(page, "Advanced AI Models");
                continue;
            }
            if (name.Contains("Locations", StringComparison.OrdinalIgnoreCase) || name.Contains("Files", StringComparison.OrdinalIgnoreCase))
            {
                PrepareV1011AdvancedPage(page, "Files & Locations");
                continue;
            }

            if (page is Container container)
                RehomeV1011Page(container, name, twoD, threeD, rig, cleanup, runtimeAdvanced);
            page.QueueFree();
        }

        HideV1011LegacyQualitySelector();
        tabs.CurrentTab = 0;
    }

    static VBoxContainer WorkflowPage(string name) => new()
    {
        Name = name,
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
    };

    void PrepareV1011AdvancedPage(Control page, string title)
    {
        CleanV1011VersionLabels(page);
        page.Name = title + " Content";
        page.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        page.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        _v1011AdvancedSettingsPages.Add((page, title));
    }

    public void AttachV1011AdvancedSettings(TabContainer settingsTabs)
    {
        foreach (var entry in _v1011AdvancedSettingsPages)
        {
            if (entry.Page.GetParent() != null) continue;
            var scroll = new ScrollContainer
            {
                Name = entry.Title,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            settingsTabs.AddChild(scroll);
            scroll.AddChild(entry.Page);
        }
    }

    void RehomeV1011Page(Container source, string sourceName, VBoxContainer twoD, VBoxContainer threeD, VBoxContainer rig, VBoxContainer cleanup, VBoxContainer advanced)
    {
        Container current = sourceName.Equals("AI", StringComparison.OrdinalIgnoreCase) ? twoD
            : sourceName.Equals("Transform", StringComparison.OrdinalIgnoreCase) ? threeD
            : cleanup;

        foreach (var node in source.GetChildren().Cast<Node>().ToList())
        {
            if (node is not Control control) continue;
            if (control is Label heading && IsV1011SectionHeading(heading))
            {
                string semantic = SemanticV1011Heading(heading.Text);
                current = DestinationForV1011Section(sourceName, semantic, twoD, threeD, rig, cleanup, advanced);
                heading.Text = semantic;
                heading.ThemeTypeVariation = "HeaderSmall";
            }
            else
            {
                string text = CollectV1011Text(control);
                if (sourceName.Equals("AI", StringComparison.OrdinalIgnoreCase))
                {
                    if (ContainsAny(text, "Install 2D AI", "Install 3D AI", "Refresh AI Component", "Unload AI Models", "Hardware: checking", "2D AI: checking", "3D AI: checking")) current = advanced;
                    else if (ContainsAny(text, "Approved 2D", "3D status:", "Generate 3D Part", "3D Patch", "Paint AI Mask", "Erase Mask", "Mask brush", "Geometry Context", "Smart Select")) current = threeD;
                    else if (ContainsAny(text, "Generate Concept", "Starting Image", "Use My Image", "Edit Starting Image", "2D Preview", "Open Last 2D", "AI Edit", "reference", "Reference")) current = twoD;
                }
                else if (sourceName.Equals("Transform", StringComparison.OrdinalIgnoreCase))
                {
                    if (ContainsAny(text, "Rig", "rig", "Joint", "joint", "Skeleton", "Pose", "pose", "IK")) current = rig;
                    if (ContainsAny(text, "Bake selected transforms", "Kitbash", "Socket", "Attachment", "Sculpt", "Mask")) current = threeD;
                }
            }

            HideNestedV1011SettingsButtons(control);
            CleanV1011VersionLabels(control);
            source.RemoveChild(control);
            current.AddChild(control);
        }
    }

    static bool IsV1011SectionHeading(Label label)
    {
        string text = label.Text?.Trim() ?? "";
        if (text.Length == 0) return false;
        if (text.Contains("— v", StringComparison.OrdinalIgnoreCase) || text.Contains(" - v", StringComparison.OrdinalIgnoreCase)) return true;
        if (label.ThemeTypeVariation.ToString().Contains("Header", StringComparison.OrdinalIgnoreCase)) return true;
        return text is "AI CREATE / MODIFY" or "INTERNET REFERENCES" or "OBJECT / KITBASH" or "POSE" or "MODEL / FINALIZE";
    }

    static string SemanticV1011Heading(string text)
    {
        string s = Regex.Replace(text.Trim(), @"\s*[—-]\s*v\d+(?:\.\d+)*.*$", "", RegexOptions.IgnoreCase).Trim();
        return s.ToUpperInvariant() switch
        {
            "AI CREATE / MODIFY" => "CONCEPT & IMAGE EDITING",
            "STARTING IMAGE" => "STARTING IMAGE",
            "AI COMPONENTS" => "AI RUNTIME & COMPONENTS",
            "INTERNET REFERENCES" => "REFERENCE SEARCH",
            "GEOMETRY-AWARE AI" => "GEOMETRY CONTEXT",
            "AI PATCH WORKFLOW" => "AI PART REFINEMENT",
            "STABILIZATION" => "JOB & GEOMETRY TOOLS",
            "RIGGING & POSING" => "RIGGING & POSING",
            "OBJECT / KITBASH" => "OBJECT TRANSFORM & KITBASH",
            "POSE" => "POSE TOOLS",
            "MODEL / FINALIZE" => "MESH FINALIZATION",
            "GEOMETRY" => "REMESH & UNION",
            _ => s
        };
    }

    static Container DestinationForV1011Section(string sourceName, string heading, VBoxContainer twoD, VBoxContainer threeD, VBoxContainer rig, VBoxContainer cleanup, VBoxContainer advanced)
    {
        if (sourceName.Equals("Print", StringComparison.OrdinalIgnoreCase) || sourceName.Equals("Model", StringComparison.OrdinalIgnoreCase)) return cleanup;
        string h = heading.ToLowerInvariant();
        if (h.Contains("runtime") || h.Contains("component") || h.Contains("provider") || h.Contains("model routing")) return advanced;
        if (h.Contains("rig") || h.Contains("pose") || h.Contains("skeleton") || h.Contains("joint")) return rig;
        if (h.Contains("starting image") || h.Contains("concept") || h.Contains("reference") || h.Contains("2d")) return twoD;
        if (h.Contains("geometry") || h.Contains("patch") || h.Contains("smart select") || h.Contains("detail") || h.Contains("kitbash") || h.Contains("sculpt") || h.Contains("attachment") || h.Contains("object transform") || h.Contains("job & geometry")) return threeD;
        if (h.Contains("final") || h.Contains("repair") || h.Contains("analysis") || h.Contains("thickness") || h.Contains("export") || h.Contains("remesh")) return cleanup;
        return sourceName.Equals("Transform", StringComparison.OrdinalIgnoreCase) ? threeD : twoD;
    }

    static string CollectV1011Text(Control control)
    {
        var parts = new List<string>();
        void Walk(Node n)
        {
            if (n is Button b) parts.Add(b.Text ?? "");
            else if (n is Label l) parts.Add(l.Text ?? "");
            else if (n is CheckButton c) parts.Add(c.Text ?? "");
            foreach (var child in n.GetChildren()) Walk(child);
        }
        Walk(control);
        return string.Join(" ", parts);
    }

    static bool ContainsAny(string text, params string[] terms) => terms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));

    void CleanV1011VersionLabels(Node root)
    {
        if (root is Label label)
        {
            if (IsV1011SectionHeading(label)) label.Text = SemanticV1011Heading(label.Text);
            else label.Text = Regex.Replace(label.Text, @"\bv\d+(?:\.\d+)+\b\s*", "", RegexOptions.IgnoreCase);
        }
        foreach (var child in root.GetChildren()) CleanV1011VersionLabels(child);
    }

    void HideNestedV1011SettingsButtons(Node root)
    {
        if (root is Button b && b.Text == "Settings" && b.GetParent() != _v1011Toolbar) b.Visible = false;
        foreach (var child in root.GetChildren()) HideNestedV1011SettingsButtons(child);
    }

    void HideV1011LegacyQualitySelector()
    {
        if (_v05Quality == null) return;
        _v05Quality.Visible = false;
        if (_v05Quality.GetParent() is Container parent)
        {
            int i = _v05Quality.GetIndex();
            if (i > 0 && parent.GetChild(i - 1) is Label label && label.Text.Contains("Quality preset", StringComparison.OrdinalIgnoreCase)) label.Visible = false;
        }
    }

    void MoveV1011SculptControlsTo3D(VBoxContainer threeD)
    {
        var brush = _brushSelect;
        var left = brush?.GetParent() as VBoxContainer;
        if (left == null) return;
        var children = left.GetChildren().Cast<Node>().ToList();
        int brushIndex = brush!.GetIndex();
        int sceneIndex = children.FindIndex(n => n is Label l && string.Equals(l.Text, "SCENE", StringComparison.OrdinalIgnoreCase));
        if (sceneIndex < 0) sceneIndex = children.Count;

        var sculptSection = new VBoxContainer { Name = "Sculpting" };
        sculptSection.AddChild(Heading("SCULPTING"));
        sculptSection.AddChild(new Label { Text = "Brush tools act directly on the selected 3D mesh in the viewport.", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        threeD.AddChild(sculptSection);

        int start = Math.Max(0, brushIndex - 1);
        for (int i = start; i < sceneIndex && i < children.Count; i++)
        {
            if (children[i] is not Control c || c == _sceneList?.GetParent()) continue;
            if (c.GetParent() != left) continue;
            if (c is Label l && string.Equals(l.Text, "SCULPT", StringComparison.OrdinalIgnoreCase)) continue;
            left.RemoveChild(c); sculptSection.AddChild(c);
        }

        foreach (var child in left.GetChildren().OfType<Label>().Where(l => string.Equals(l.Text, "SCULPT", StringComparison.OrdinalIgnoreCase)).ToList()) child.QueueFree();
    }

    void EnforceV1011ApprovedBaseline()
    {
        if (_v109Generate3D == null) return;
        _v109Generate3D.Disabled = true;
        _v109Generate3D.Text = "Accept a 2D Baseline First";
        _v109Generate3D.ButtonDown += () =>
        {
            if (!string.IsNullOrWhiteSpace(_v1011BaselineImage) && File.Exists(_v1011BaselineImage))
                _lastEditedImage = _v1011BaselineImage;
        };
    }

    void AcceptV1011Baseline()
    {
        string candidate = !string.IsNullOrWhiteSpace(_lastEditedImage) && File.Exists(_lastEditedImage) ? _lastEditedImage
            : !string.IsNullOrWhiteSpace(_v03StartingImage) && File.Exists(_v03StartingImage) ? _v03StartingImage
            : !string.IsNullOrWhiteSpace(_lastCapture) && File.Exists(_lastCapture) ? _lastCapture : "";
        if (string.IsNullOrWhiteSpace(candidate))
        {
            if (_v1011BaselineStatus != null) _v1011BaselineStatus.Text = "Baseline: no usable image yet — generate or load one first.";
            SetStatus("Generate a concept or load your own starting image before accepting the baseline.");
            return;
        }
        _v1011BaselineImage = Path.GetFullPath(candidate);
        _lastEditedImage = _v1011BaselineImage;
        if (_v1011BaselineStatus != null) _v1011BaselineStatus.Text = "Baseline accepted: " + Path.GetFileName(_v1011BaselineImage);
        if (_v109Generate3D != null)
        {
            _v109Generate3D.Disabled = false;
            _v109Generate3D.Text = "Generate 3D from Accepted Baseline";
        }
        SetStatus("2D baseline accepted. Continue to the 3D tab.");
    }

    internal string V1011Approved2DSource() => !string.IsNullOrWhiteSpace(_v1011BaselineImage) && File.Exists(_v1011BaselineImage) ? _v1011BaselineImage : "";

    void RepairV1011ViewportAtLaunch()
    {
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host || FindChild("Viewport", true, false) is not SubViewport sub) return;
        host.Stretch = true;
        sub.TransparentBg = false;
        sub.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        host.Resized -= QueueV1011ViewportRepair;
        host.Resized += QueueV1011ViewportRepair;
        QueueV1011ViewportRepair();
    }

    void QueueV1011ViewportRepair()
    {
        // Compatibility-only repair for pre-v1.0.19 composition. Once the native Stretch
        // pipeline is installed, resize must not rebuild the world/grid/environment.
        if (_v1019ViewportPipelineInstalled) return;
        CallDeferred(nameof(FinishV1011ViewportRepair));
    }

    void FinishV1011ViewportRepair()
    {
        if (_v1019ViewportPipelineInstalled) return;
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host || FindChild("Viewport", true, false) is not SubViewport sub) return;
        Vector2 size = host.Size;
        sub.Size = new Vector2I(Math.Max(1, (int)Math.Round(size.X)), Math.Max(1, (int)Math.Round(size.Y)));
        sub.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        if (_camera != null)
        {
            _camera.Current = false;
            _camera.Current = true;
            UpdateCamera();
        }
        if (_world == null) return;

        foreach (var plane in _world.GetChildren().OfType<MeshInstance3D>().Where(m => m.Mesh is PlaneMesh && !_objects.Contains(m))) plane.Visible = false;
        if (_v109GridRoot != null && IsInstanceValid(_v109GridRoot)) _v109GridRoot.QueueFree();
        _v109GridRoot = new Node3D { Name = "Viewport Grid" };
        _world.AddChild(_v109GridRoot);
        AddV1011Grid(10f, new Color(.42f, .46f, .53f), false);
        AddV1011Grid(50f, new Color(.68f, .70f, .76f), true);
        AddV109Axis("X axis", new Vector3(-250, .12f, 0), new Vector3(250, .12f, 0), new Color(.90f, .30f, .26f));
        AddV109Axis("Z axis", new Vector3(0, .12f, -250), new Vector3(0, .12f, 250), new Color(.28f, .55f, .95f));
        if (_world.GetChildren().OfType<WorldEnvironment>().FirstOrDefault()?.Environment is Godot.Environment env)
        {
            env.BackgroundMode = Godot.Environment.BGMode.Color;
            env.BackgroundColor = new Color(.035f, .041f, .052f);
            env.AmbientLightColor = new Color(.46f, .48f, .54f);
            env.AmbientLightEnergy = 1.0f;
        }
    }

    void AddV1011Grid(float spacing, Color color, bool majorsOnly)
    {
        if (_v109GridRoot == null) return;
        var mesh = new ImmediateMesh();
        var mat = new StandardMaterial3D { AlbedoColor = color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, mat);
        for (float p = -250; p <= 250.01f; p += spacing)
        {
            if (!majorsOnly && Math.Abs(p % 50f) < .01f) continue;
            mesh.SurfaceAddVertex(new Vector3(p, .10f, -250)); mesh.SurfaceAddVertex(new Vector3(p, .10f, 250));
            mesh.SurfaceAddVertex(new Vector3(-250, .10f, p)); mesh.SurfaceAddVertex(new Vector3(250, .10f, p));
        }
        mesh.SurfaceEnd();
        _v109GridRoot.AddChild(new MeshInstance3D { Name = majorsOnly ? "Major grid" : "Minor grid", Mesh = mesh });
    }
}
