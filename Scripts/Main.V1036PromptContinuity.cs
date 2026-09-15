using Godot;
using Miniscuplter.Core;
using System;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    const string V1036PromptMetadataKey = "ui.last_prompt";
    ProjectId? _v1036PromptProjectId;
    string _v1036LastPersistedPrompt = "";
    Timer? _v1036PromptContinuityTimer;
    bool _v1036PromptSaveInFlight;

    public void InstallV1036PromptContinuity()
    {
        if (_v1036PromptContinuityTimer != null || _prompt == null) return;
        _v1036PromptContinuityTimer = new Timer
        {
            Name = "v1.0.36 Prompt Continuity",
            WaitTime = .75,
            OneShot = false,
            Autostart = true
        };
        _v1036PromptContinuityTimer.Timeout += V1036ReconcilePromptContinuity;
        AddChild(_v1036PromptContinuityTimer);
    }

    void V1036ReconcilePromptContinuity()
    {
        if (_prompt == null || _v1020StageCSession == null || _v1036PromptSaveInFlight) return;
        ProjectState current = _v1020StageCSession.Current;
        if (_v1036PromptProjectId is null || _v1036PromptProjectId.Value != current.ProjectId)
        {
            _v1036PromptProjectId = current.ProjectId;
            string restored = current.Metadata.TryGetValue(V1036PromptMetadataKey, out string? value) ? value : "";
            _v1036LastPersistedPrompt = restored;
            if (_prompt.Text != restored)
                _prompt.Text = restored;
            return;
        }

        string prompt = _prompt.Text ?? "";
        if (prompt == _v1036LastPersistedPrompt) return;
        _ = V1036PersistPromptAsync(prompt);
    }

    async Task V1036PersistPromptAsync(string prompt)
    {
        if (_v1020StageCSession == null || _v1036PromptSaveInFlight) return;
        ProjectId expectedProjectId = _v1020StageCSession.Current.ProjectId;
        _v1036PromptSaveInFlight = true;
        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                ProjectSession session = _v1020StageCSession ?? throw new InvalidOperationException("Project session is unavailable.");
                if (session.Current.ProjectId != expectedProjectId) return;
                string durable = session.Current.Metadata.TryGetValue(V1036PromptMetadataKey, out string? value) ? value : "";
                if (durable != prompt)
                {
                    session.Execute("Update concept prompt", state => state.WithMetadata(V1036PromptMetadataKey, prompt));
                    await V1020SaveSessionAsync();
                }
                _v1036LastPersistedPrompt = prompt;
            }
            finally { _v1020StageCGate.Release(); }
        }
        catch (Exception ex)
        {
            SetStatus("Prompt continuity save failed safely: " + ex.Message);
        }
        finally { _v1036PromptSaveInFlight = false; }
    }
}
