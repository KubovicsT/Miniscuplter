namespace Miniscuplter;

public partial class Main
{
    // Optional parameters do not participate in method-group conversion to Action.
    // Keep the bool overload for internal silent cleanup and expose an exact callback overload for UI/commands.
    void V098DiscardDetail() => V098DiscardDetail(true);
}
