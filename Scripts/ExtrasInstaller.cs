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
        if (GetParent() is Main main) main.InstallV01Extras();
    }
}
