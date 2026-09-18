using Godot;
using Miniscuplter.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    ProjectId? _v1036PromptProjectId;
    RevisionId? _v1036PromptImageRevisionId;
    string _v1036LastPersistedPrompt = "";
    Timer? _v1036PromptContinuityTimer;
    bool _v1036PromptSaveInFlight;
    bool _v1036AcceptedPromptPresented;

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
            _v1036PromptImageRevisionId = null;
            _v1036AcceptedPromptPresented = false;
        }

        RevisionId? acceptedRevisionId;
        string restored;
        try { restored = StageCPromptBinding.ReadAcceptedPrompt(current, out acceptedRevisionId); }
        catch (Exception ex)
        {
            _v1036AcceptedPromptPresented = false;
            SetStatus("Prompt continuity is unavailable because accepted image identity is invalid: " + ex.Message);
            return;
        }
        if (acceptedRevisionId is not { } revisionId || !V1036IsAcceptedRevisionPresented(current, revisionId))
        {
            _v1036AcceptedPromptPresented = false;
            return;
        }
        if (!_v1036AcceptedPromptPresented || _v1036PromptImageRevisionId != revisionId)
        {
            _v1036AcceptedPromptPresented = true;
            _v1036PromptImageRevisionId = revisionId;
            _v1036LastPersistedPrompt = restored;
            if (_prompt.Text != restored) _prompt.Text = restored;
            if (StageCPromptBinding.NeedsLegacyMigration(current, revisionId))
                _ = V1036PersistPromptAsync(revisionId, restored);
            return;
        }

        string prompt = _prompt.Text ?? "";
        if (prompt == _v1036LastPersistedPrompt) return;
        _ = V1036PersistPromptAsync(revisionId, prompt);
    }

    bool V1036IsAcceptedRevisionPresented(ProjectState state, RevisionId revisionId)
    {
        if (!state.ImageRevisions.TryGetValue(revisionId, out ImageRevision? revision) ||
            string.IsNullOrWhiteSpace(_v1020StageCProjectPath) ||
            string.IsNullOrWhiteSpace(_lastEditedImage)) return false;
        try
        {
            string acceptedPath = StageCAssetStore.ResolveImagePath(_v1020StageCProjectPath, revision);
            return Path.GetFullPath(acceptedPath).Equals(
                Path.GetFullPath(_lastEditedImage), StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    async Task V1036PersistPromptAsync(RevisionId expectedImageRevisionId, string prompt)
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
                RevisionId? currentAccepted = StageCGeneration.AcceptedBaseline(session.Current);
                if (currentAccepted != expectedImageRevisionId ||
                    !V1036IsAcceptedRevisionPresented(session.Current, expectedImageRevisionId)) return;
                string durable = StageCPromptBinding.ReadPrompt(session.Current, expectedImageRevisionId);
                if (durable != prompt || StageCPromptBinding.NeedsLegacyMigration(session.Current, expectedImageRevisionId))
                {
                    StageCPromptBinding.SetAcceptedPrompt(session, expectedImageRevisionId, prompt);
                    await V1020SaveSessionAsync();
                }
                if (_v1036PromptImageRevisionId == expectedImageRevisionId)
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
