using Godot;
using Miniscuplter.Core;
using System;
using System.IO;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV1036ProjectResetIsolation()
    {
        var root = GetChildren().OfType<VBoxContainer>().FirstOrDefault();
        var toolbar = root?.GetChildren().OfType<HBoxContainer>().FirstOrDefault();
        if (toolbar != null)
        {
            var newButton = toolbar.GetChildren().OfType<Button>().FirstOrDefault(b => b.Text == "New");
            if (newButton != null)
            {
                newButton.Pressed -= V1036ClearProjectPresentation;
                newButton.Pressed += V1036ClearProjectPresentation;
            }

            // The legacy project actions were appended after a crowded modeling toolbar, which can
            // clip Open/Load entirely on ordinary window widths. Keep the existing handler but make
            // the project-open action a first-class neighbor of New.
            var openButton = toolbar.GetChildren().OfType<Button>().FirstOrDefault(b => b.Text is "Load Project" or "Open Project");
            if (openButton != null)
            {
                openButton.Text = "Open Project";
                toolbar.MoveChild(openButton, Math.Min(1, toolbar.GetChildCount() - 1));
            }
        }

        if (FindChild("ViewportHost", true, false) is SubViewportContainer host)
        {
            host.GuiInput -= V1036StabilizeOrbitPivot;
            host.GuiInput += V1036StabilizeOrbitPivot;

            var tabs = (host.GetParent() as HSplitContainer)?.GetChildren().OfType<TabContainer>().FirstOrDefault();
            if (tabs != null)
            {
                // v1.0.19 framed the selected object every time a non-2D tab became active. That
                // turns an ordinary tab switch into an implicit camera reset. Preserve the user's
                // orbit/zoom and refresh presentation without mutating camera focus/distance.
                tabs.TabChanged -= V1019WorkflowTabChanged;
                tabs.TabChanged -= V1036WorkflowTabChangedPreserveCamera;
                tabs.TabChanged += V1036WorkflowTabChangedPreserveCamera;
            }
        }
    }

    void V1036StabilizeOrbitPivot(InputEvent ev)
    {
        if (ev is not InputEventMouseButton button || button.ButtonIndex != MouseButton.Right || !button.Pressed) return;
        if (_selected == null || !IsInstanceValid(_selected)) return;

        var bounds = _selected.GetAabb();
        _focus = _selected.GlobalTransform * (bounds.Position + bounds.Size * .5f);
        UpdateCamera();
    }

    void V1036WorkflowTabChangedPreserveCamera(long tab)
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
        V1017UpdateGizmo();
        if (FindChild("Viewport", true, false) is SubViewport sub) V1019UpdateViewportDiagnostics(sub);
        V1019ArmRenderProbe();
    }

    async void V1036ClearProjectPresentation()
    {
        // NewScene owns scene-object reset. This companion runs after that handler and retires
        // project-scoped 2D/prompt-selection presentation that historically survived New.
        _lastCapture = "";
        _lastEditedImage = "";
        _lastMask = "";
        _v03StartingImage = "";
        _v1011BaselineImage = "";
        _v1020GenerationBinding = null;
        _v1020PendingCandidate = null;
        _v1020PendingCandidateMesh = null;

        if (_aiPreview != null) _aiPreview.Texture = null;
        SyncV1015CanvasSource("");
        _v1015ImageCanvas?.ClearSelection();
        V1020RefreshCandidateControls();
        if (_v1011BaselineStatus != null) _v1011BaselineStatus.Text = "Baseline: not accepted yet";
        if (_v1015EditStatus != null) _v1015EditStatus.Text = "2D source: generate or load an image first.";
        if (_v109Generate3D != null)
        {
            _v109Generate3D.Disabled = true;
            _v109Generate3D.Text = "Accept a 2D Baseline First";
        }

        // The Stage-C compatibility bridge owns durable accepted-baseline/candidate state. New must
        // retire that state too; otherwise accepting a new image can silently inherit the previous
        // project's revisions from stagec_working.msculpt2 even though the viewport looks fresh.
        await _v1020StageCGate.WaitAsync();
        try
        {
            string projects = AppDataRoot.Resolve("projects");
            Directory.CreateDirectory(projects);
            _v1020StageCProjectPath = Path.Combine(projects, "stagec_working.msculpt2");
            var fresh = ProjectState.Create("Stage C working project")
                .WithMetadata("compatibility_bridge", "v1.0.20-editor");
            await _v1020StageCStore.SaveAsync(fresh, _v1020StageCProjectPath);
            _v1020StageCSession = new ProjectSession(fresh);
            _v1020StageCSession.MarkSaved();
        }
        catch (Exception ex)
        {
            _v1020StageCSession = null;
            SetStatus("New scene presentation is clear, but fresh durable project state could not be established: " + ex.Message);
        }
        finally { _v1020StageCGate.Release(); }
    }
}
