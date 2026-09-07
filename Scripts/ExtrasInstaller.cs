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
        // their controls silently. Restore the compatibility name before any extras install.
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
        main.InstallV108AiFeedback();
        main.InstallV109Experience();

        // v1.0.11 consolidates the visible workflow after historical controls have installed.
        main.InstallV1011Workflow();
        main.InstallV109ResponsiveLayout();

        // v1.0.12 is a safety bridge: patch the final composed UI so export/autosave and other
        // critical entry points cannot fall back to older unguarded implementations.
        main.InstallV1012SafetyBridge();

        // v1.0.13 starts the replacement project model beside the legacy editor. The bridge only
        // creates new .msculpt2 copies and never replaces the proven .msculpt path while migration
        // and semantic adapters are still being validated.
        main.InstallV1013FoundationBridge();

        // Current Python model adapters are synchronous. Cancelling only the editor's HTTP request
        // leaves the server-side CUDA work alive, so every cancel action is wired to an owned
        // backend-process restart and all following jobs wait for that health recovery.
        main.InstallV1013CancellationRecovery();

        // v1.0.15 makes the first end-to-end creative slice testable on the target machine: the
        // center workspace becomes the actual 2D editing canvas in the 2D tab, references become
        // visible/selectable images, and the 3D workspace gets a driver-robust triangle grid.
        main.InstallV1015ThinSlice();

        // v1.0.16 replaces only the visible reference-search surface after the v1.0.15 canvas is
        // installed. The older Commons implementation remains underneath as a rollback-safe layer.
        main.InstallV1016ReferenceSearch();
    }
}