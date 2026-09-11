using Godot;

namespace Miniscuplter;

public partial class ExtrasInstaller : Node
{
    public override void _Ready() { CallDeferred(MethodName.Install); }

    void Install()
    {
        if (GetParent() is not Main main) return;
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
        main.InstallV1011Workflow();
        main.InstallV109ResponsiveLayout();
        main.InstallV1012SafetyBridge();
        main.InstallV1013FoundationBridge();
        main.InstallV1013CancellationRecovery();
        main.InstallV1015ThinSlice();
        main.InstallV1016ReferenceSearch();
        main.InstallV1017Usability();
        main.InstallV1019ViewportPipeline();
        main.InstallV1020StageCBridge();
        main.InstallV1020AppliedObjectRestore();
        main.InstallV1020StageCCleanupExport();
        main.InstallV1020StageCEditingAuthority();
        main.InstallV1022Acceptance();

        // v1.0.23 MS-027 remains presentation-only and composes after accepted viewport ownership.
        main.InstallV1023UiPreferences();
        main.InstallV1023ViewportToolStrip();
        main.InstallSceneHierarchy();
        main.InstallViewCube();
        main.InstallAiCommandConsole();
        main.InstallResourceTelemetry();
        main.InstallWorkspaceDensity();
    }
}
