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
    }
}
