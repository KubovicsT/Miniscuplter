using Godot;

namespace Miniscuplter;

public partial class ExtrasInstaller : Node
{
    public override void _Ready() { CallDeferred(MethodName.Install); }

    void Install()
    {
        if (GetParent() is not Main main) return;

        // The release-facing tab is called "Model", but all additive version installers from
        // v0.4 onward target the stable internal node name "Print". A base-UI rename in v1.0
        // accidentally changed the node itself to "Model", causing those installers to skip
        // their controls silently. Restore the compatibility name before any extras install;
        // InstallV100ReleasePolish() still presents the tab to users as "Model".
        if (main.FindChild("Print", true, false) == null && main.FindChild("Model", true, false) is Control modelTab)
            modelTab.Name = "Print";

        main.InstallV01Extras();
        main.InstallV02Extras();
        main.InstallV03Extras();
        main.InstallV04Extras();
        main.InstallV05Extras();
        main.InstallV055Extras();
        main.InstallV06Extras();
        main.InstallV07Extras();
        main.InstallV07Follow();
        main.InstallV08Extras();
        main.InstallV08Hook();
        main.InstallV09Extras();
        main.InstallV09Thickness();
        main.InstallV095Stability();
        main.InstallV095IntegrityGuards();
        main.InstallV095TopologyGuards();
        main.InstallV095RigGuards();
        main.InstallV095LibraryGuards();
        main.InstallV095AttachmentGuards();
        main.InstallV095LoadGuards();
        main.InstallV095ExportGuards();
        main.InstallV096SmartSelect();
        main.InstallV096CommandPalette();
        main.InstallV096SemanticComponent();
        main.InstallV097QualityPresets();
        main.InstallV098MultiModelAI();
        main.InstallV099Locations();
        main.InstallV100ReleasePolish();
    }
}
