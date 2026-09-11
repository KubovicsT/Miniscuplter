using Godot;
using System;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    bool _v1019ViewportPipelineInstalled;
    bool _v1019WorldConfigured;
    Timer? _v1019ViewportProbeTimer;
    Label? _v1019ViewportDiagnostics;

    public void InstallV1019ViewportPipeline()
    {
        if (_v1019ViewportPipelineInstalled) return;
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host ||
            FindChild("Viewport", true, false) is not SubViewport)
            return;

        InstallV1018ViewportFoundation();
        if (_world == null) return;

        _v1019ViewportPipelineInstalled = true;
        // v1.0.9 attached a host-resize writer before the native Stretch pipeline existed.
        // Retire that exact historical owner now; leaving it subscribed made splitter resize
        // mutate the render target behind Stretch and reproduced the user's resize-only render change.
        host.Resized -= V109SyncLegacyViewportSize;
        host.Resized -= QueueV1011ViewportRepair;
        // v1.0.17's watchdog used to write SubViewport.Size every 500 ms. Once the native
        // Stretch-owned pipeline is authoritative that timer must no longer participate in layout.
        _v1017ViewportTimer?.Stop();

        host.Visible = true;
        host.Stretch = true;
        host.ClipContents = true;
        host.MouseFilter = Control.MouseFilterEnum.Stop;
        host.Modulate = Colors.White;
        host.SelfModulate = Colors.White;
        host.CustomMinimumSize = new Vector2(320, 240);

        if (FindChild("3D", true, false) is VBoxContainer threeD &&
            _v1019ViewportDiagnostics == null)
        {
            _v1019ViewportDiagnostics = new Label
            {
                Name = "v1.0.19 Viewport Diagnostics",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            threeD.AddChild(_v1019ViewportDiagnostics);
        }

        _v1019ViewportProbeTimer = new Timer
        {
            Name = "v1.0.19 Viewport Render Probe",
            WaitTime = 1.0,
            OneShot = true
        };
        _v1019ViewportProbeTimer.Timeout += V1019ProbeRenderedFrame;
        AddChild(_v1019ViewportProbeTimer);

        // Stretch=true makes the SubViewportContainer the normal resize owner. A full repair after
        // every splitter resize used to rebuild/rebind render state after the drag settled, which
        // made the resting frame differ from the frame shown while dragging. Normal resize now only
        // refreshes diagnostics/probing; V1019RepairViewportPipeline is recovery-only.
        host.Resized += () =>
        {
            if (FindChild("Viewport", true, false) is SubViewport sub)
                V1019UpdateViewportDiagnostics(sub);
            V1019ArmRenderProbe();
        };

        // v1.0.17 used a full world repair on every workflow-tab change. Replace that handler once
        // the native pipeline owns the viewport: normal tab changes may frame/refresh presentation,
        // but they must not recreate/rebind world or layout state.
        var tabs = (host.GetParent() as HSplitContainer)?.GetChildren().OfType<TabContainer>().FirstOrDefault();
        if (tabs != null)
        {
            tabs.TabChanged -= V1017WorkflowTabChanged;
            tabs.TabChanged += V1019WorkflowTabChanged;
        }

        V1019RepairViewportPipeline();
        V1019ArmRenderProbe();
    }

    void V1019WorkflowTabChanged(long tab)
    {
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host) return;
        var tabs = (host.GetParent() as HSplitContainer)?.GetChildren().OfType<TabContainer>().FirstOrDefault();
        string title = tabs != null && tabs.GetTabCount() > 0
            ? tabs.GetTabTitle(Math.Clamp((int)tab, 0, tabs.GetTabCount() - 1))
            : "";
        if (title.Equals("2D", StringComparison.OrdinalIgnoreCase)) return;

        if (_v1015ImageCanvas != null) _v1015ImageCanvas.Visible = false;
        if (_v1015CanvasHint != null) _v1015CanvasHint.Visible = false;
        V1019ConfigureStudioLighting();
        if (_selected != null && GodotObject.IsInstanceValid(_selected))
            CallDeferred(nameof(V1019FrameSelectedStable));
        else
            V1019ArmRenderProbe();
    }

    void V1019FrameSelectedStable()
    {
        if (_selected != null && GodotObject.IsInstanceValid(_selected))
            FrameSelected();
        V1017UpdateGizmo();
        if (FindChild("Viewport", true, false) is SubViewport sub)
            V1019UpdateViewportDiagnostics(sub);
        V1019ArmRenderProbe();
    }

    void V1019ArmRenderProbe()
    {
        if (!_v1019ViewportPipelineInstalled || _v1019ViewportProbeTimer == null) return;
        _v1019ViewportProbeTimer.Stop();
        _v1019ViewportProbeTimer.Start();
    }

    public void V1019RepairViewportPipeline()
    {
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host ||
            FindChild("Viewport", true, false) is not SubViewport sub ||
            _world == null)
            return;

        host.Visible = true;
        host.Stretch = true;
        host.ClipContents = true;
        host.Modulate = Colors.White;
        host.SelfModulate = Colors.White;

        // OwnWorld3D is established before the scene root is rebound. Do not assign a separate
        // World3D object and then also enable OwnWorld3D: that creates competing world ownership.
        // Reparent once when first configuring the native pipeline (or only if recovery finds the
        // world under the wrong parent), then leave the scene graph stable on ordinary operations.
        sub.OwnWorld3D = true;
        sub.Disable3D = false;
        sub.TransparentBg = false;
        sub.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

        bool needsWorldRebind = !_v1019WorldConfigured || !ReferenceEquals(_world.GetParent(), sub);
        if (needsWorldRebind)
        {
            _world.GetParent()?.RemoveChild(_world);
            sub.AddChild(_world);
            _v1019WorldConfigured = true;
        }
        _world.Visible = true;

        if (_camera == null || !GodotObject.IsInstanceValid(_camera))
        {
            _camera = new Camera3D
            {
                Current = true,
                Near = .01f,
                Far = 10000f,
                Fov = 45f
            };
            _world.AddChild(_camera);
        }
        else if (!ReferenceEquals(_camera.GetParent(), _world))
        {
            _camera.GetParent()?.RemoveChild(_camera);
            _world.AddChild(_camera);
        }

        _camera.Visible = true;
        _camera.Near = .01f;
        _camera.Far = 10000f;
        _camera.Current = false;
        _camera.Current = true;
        UpdateCamera();

        V1019ConfigureStudioLighting();

        foreach (var plane in _world.GetChildren().OfType<MeshInstance3D>()
                     .Where(m => !_objects.Contains(m)).ToArray())
            plane.Visible = false;

        _objects.RemoveAll(o => !GodotObject.IsInstanceValid(o));
        if (_objects.Count == 0)
            AddStarterMesh();

        if (_selected == null || !GodotObject.IsInstanceValid(_selected) || !_objects.Contains(_selected))
        {
            _selected = _objects.FirstOrDefault();
            if (_selected != null) Select(_selected);
        }

        foreach (var obj in _objects.ToArray())
        {
            if (!GodotObject.IsInstanceValid(obj)) continue;
            if (!ReferenceEquals(obj.GetParent(), _world))
            {
                obj.GetParent()?.RemoveChild(obj);
                _world.AddChild(obj);
            }
            obj.Visible = true;
            obj.Layers = 1;
            if (obj.MaterialOverride is not StandardMaterial3D material)
            {
                material = new StandardMaterial3D();
                obj.MaterialOverride = material;
            }
            material.AlbedoColor = ReferenceEquals(obj, _selected)
                ? new Color(.66f, .66f, .66f)
                : new Color(.55f, .55f, .55f);
            material.Roughness = .68f;
            material.Metallic = 0f;
            material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        }

        if (_v109GridRoot == null || !GodotObject.IsInstanceValid(_v109GridRoot) ||
            !_v109GridRoot.Name.ToString().Contains("v1.0.19", StringComparison.Ordinal))
            V1019RebuildGrid();

        if (_v1017Gizmo == null || !GodotObject.IsInstanceValid(_v1017Gizmo))
            V1017BuildGizmo();
        V1017UpdateGizmo();
        V1017UpdateViewportStatus();
        V1019UpdateViewportDiagnostics(sub);
        V1019ArmRenderProbe();
    }

    void V1019ConfigureStudioLighting()
    {
        if (_world == null) return;

        var envNode = _world.GetChildren().OfType<WorldEnvironment>().FirstOrDefault();
        if (envNode == null)
        {
            envNode = new WorldEnvironment { Name = "Viewport Studio Environment" };
            envNode.Environment = new Godot.Environment();
            _world.AddChild(envNode);
        }
        envNode.Environment ??= new Godot.Environment();
        var env = envNode.Environment;
        env.BackgroundMode = Godot.Environment.BGMode.Color;
        env.BackgroundColor = new Color(.115f, .115f, .115f);
        env.AmbientLightSource = Godot.Environment.AmbientSource.Color;
        env.AmbientLightColor = new Color(.78f, .78f, .78f);
        env.AmbientLightEnergy = .72f;

        var key = _world.GetChildren().OfType<DirectionalLight3D>().FirstOrDefault();
        if (key == null)
        {
            key = new DirectionalLight3D { Name = "Viewport Studio Key" };
            _world.AddChild(key);
        }
        key.RotationDegrees = new Vector3(-50f, -35f, 0f);
        key.LightColor = new Color(1f, .98f, .95f);
        key.LightEnergy = 1.05f;
        key.ShadowEnabled = true;

        var fill = _world.GetChildren().OfType<OmniLight3D>().FirstOrDefault();
        if (fill == null)
        {
            fill = new OmniLight3D { Name = "Viewport Studio Fill" };
            _world.AddChild(fill);
        }
        fill.Position = new Vector3(-55f, 75f, 70f);
        fill.OmniRange = 300f;
        fill.LightColor = new Color(.82f, .88f, 1f);
        fill.LightEnergy = 1.35f;
    }

    void V1019RebuildGrid()
    {
        if (_world == null) return;
        if (_v109GridRoot != null && GodotObject.IsInstanceValid(_v109GridRoot))
            _v109GridRoot.QueueFree();

        bool visible = _v109GridToggle?.ButtonPressed ?? true;
        _v109GridRoot = new Node3D
        {
            Name = "Viewport Grid v1.0.19",
            Visible = visible
        };
        _world.AddChild(_v109GridRoot);

        var ground = new MeshInstance3D
        {
            Name = "Grid ground v1.0.19",
            Mesh = new PlaneMesh { Size = new Vector2(500, 500) },
            Position = new Vector3(0, -.03f, 0),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(.145f, .145f, .145f),
                Roughness = 1f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            }
        };
        _v109GridRoot.AddChild(ground);

        // Triangle bars avoid driver-specific line-width behavior while matching a neutral
        // modeling-viewport hierarchy: quiet minor grid, stronger major grid, colored X/Z axes.
        AddV1015GridBars(10f, .14f, new Color(.29f, .29f, .29f), false);
        AddV1015GridBars(50f, .38f, new Color(.43f, .43f, .43f), true);
        AddV1015AxisBar(true, .76f, new Color(.90f, .22f, .18f));
        AddV1015AxisBar(false, .76f, new Color(.20f, .42f, .92f));
        _v109GridRoot.Visible = visible;
    }

    void V1019UpdateViewportDiagnostics(SubViewport sub)
    {
        if (_v1019ViewportDiagnostics == null) return;
        string worldParent = _world == null ? "none" : _world.GetParent()?.Name.ToString() ?? "none";
        string grid = _v109GridRoot != null && GodotObject.IsInstanceValid(_v109GridRoot)
            ? $"{_v109GridRoot.Name} ({_v109GridRoot.GetChildCount()} render groups)"
            : "missing";
        string selected = _selected == null ? "none" : _selected.Name.ToString();
        int keyLights = _world?.GetChildren().OfType<DirectionalLight3D>().Count() ?? 0;
        int fillLights = _world?.GetChildren().OfType<OmniLight3D>().Count() ?? 0;
        _v1019ViewportDiagnostics.Text =
            "Viewport pipeline: native SubViewportContainer · resize owner: Stretch" + Environment.NewLine +
            $"Render target: {sub.Size.X}×{sub.Size.Y} · 3D {(sub.Disable3D ? "disabled" : "active")} · update Always" + Environment.NewLine +
            $"World parent: {worldParent} · own World3D: {sub.OwnWorld3D} · configured: {_v1019WorldConfigured}" + Environment.NewLine +
            $"Lighting: {keyLights} key / {fillLights} fill · Grid: {grid} · scene objects: {_objects.Count} · selected: {selected}";
    }

    async void V1019ProbeRenderedFrame()
    {
        try
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            if (FindChild("Viewport", true, false) is not SubViewport sub) return;
            Image image = sub.GetTexture().GetImage();
            if (image == null || image.IsEmpty() || image.GetWidth() < 2 || image.GetHeight() < 2)
            {
                if (_v1019ViewportDiagnostics != null)
                    _v1019ViewportDiagnostics.Text += Environment.NewLine +
                        "Render probe: no readable frame yet; viewport will retry after layout.";
                return;
            }

            Color corner = image.GetPixel(1, 1);
            Color center = image.GetPixel(image.GetWidth() / 2, image.GetHeight() / 2);
            Color lower = image.GetPixel(image.GetWidth() / 2, Math.Max(1, image.GetHeight() - 2));
            float variation = V1019ColorDelta(corner, center) + V1019ColorDelta(corner, lower);
            float backgroundLuma = V1019Luminance(corner);
            float centerLuma = V1019Luminance(center);
            if (_v1019ViewportDiagnostics != null)
                _v1019ViewportDiagnostics.Text += Environment.NewLine +
                    (variation > .03f
                        ? $"Render probe: non-flat frame · background luma {backgroundLuma:0.00} · center {centerLuma:0.00}."
                        : "Render probe: frame is flat; inspect world/camera/material state above.");
        }
        catch (Exception ex)
        {
            if (_v1019ViewportDiagnostics != null)
                _v1019ViewportDiagnostics.Text += Environment.NewLine + "Render probe failed: " + ex.Message;
        }
    }

    static float V1019ColorDelta(Color a, Color b) =>
        Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) + Math.Abs(a.A - b.A);

    static float V1019Luminance(Color c) => .2126f * c.R + .7152f * c.G + .0722f * c.B;
}
