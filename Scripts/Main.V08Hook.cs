namespace Miniscuplter;

public partial class Main
{
    public void InstallV08Hook()
    {
        SculptEngine.ApplyOverride = ApplyV08Sculpt;
    }
}
