using Godot;
using System;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV1036CameraContinuity()
    {
        if (FindChild("ViewportHost", true, false) is not SubViewportContainer host) return;
        var tabs = (host.GetParent() as HSplitContainer)?.GetChildren().OfType<TabContainer>().FirstOrDefault();
        if (tabs == null) return;

        // v1.0.19 still framed the selected object after every non-2D workflow-tab change.
        // That rewrote camera orbit state even though tab changes are presentation-only.
        tabs.TabChanged -= V1019WorkflowTabChanged;
        tabs.TabChanged -= V1036WorkflowTabChangedPreserveCamera;
        tabs.TabChanged += V1036WorkflowTabChangedPreserveCamera;
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
        if (FindChild("Viewport", true, false) is SubViewport sub)
            V1019UpdateViewportDiagnostics(sub);
        V1019ArmRenderProbe();
    }
}
