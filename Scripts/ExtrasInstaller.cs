using Godot;

namespace Miniscuplter;

public partial class ExtrasInstaller : Node
{
    public override void _Ready()
    {
        CallDeferred(MethodName.Install);
    }

    void Install()
    {
        if (GetParent() is not Main main) return;
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
    }
}
