using Godot;
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
        // Tab changes are presentation navigation and must not rewrite camera orbit/framing state.
        tabs.TabChanged -= V1017WorkflowTabChanged;
        tabs.TabChanged -= V1019WorkflowTabChanged;
        tabs.TabChanged -= V1036WorkflowTabChangedPreserveCamera;
        tabs.TabChanged += V1036WorkflowTabChangedPreserveCamera;
    }
}
