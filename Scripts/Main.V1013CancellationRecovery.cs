using Godot;
using System;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV1013CancellationRecovery()
    {
        _ai.CancellationRecoveryHandler = RecoverV1013BackendAfterCancellationAsync;
    }

    async Task RecoverV1013BackendAfterCancellationAsync()
    {
        var launcher = GetNodeOrNull<BackendLauncher>("BackendLauncher")
            ?? throw new InvalidOperationException("The owned local AI backend launcher is unavailable. Restart Miniscuplter before starting another AI job.");

        // HttpClient cancellation only disconnects the editor from FastAPI. Most current model
        // adapters execute synchronously in the backend process, so that Python/CUDA work would
        // otherwise continue in the background and collide with the next request. Restarting the
        // owned process tree is the reliable hard-cancel boundary until the Stage-B Job Broker
        // moves providers into individually cancellable worker processes.
        await launcher.RestartAsync();

        Exception? last = null;
        for (int attempt = 0; attempt < 80; attempt++)
        {
            try
            {
                if (await _ai.ProbeHealthAsync()) return;
            }
            catch (Exception ex) { last = ex; }
            await Task.Delay(250);
        }

        throw new InvalidOperationException("The local AI backend did not become healthy within 20 seconds after cancellation." +
            (last == null ? "" : " " + last.Message));
    }
}
