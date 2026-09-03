using Godot;

namespace Miniscuplter;

public partial class Main
{
    Timer? _v07FollowTimer;

    public void InstallV07Follow()
    {
        _v07FollowTimer = new Timer { WaitTime = 0.2, OneShot = false, Autostart = true };
        _v07FollowTimer.Timeout += () =>
        {
            if (_v07Attachments.Count > 0 || _v07Sockets.Count > 0) RefreshV07Attachments();
        };
        AddChild(_v07FollowTimer);
    }
}
