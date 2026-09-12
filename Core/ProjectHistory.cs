namespace Miniscuplter.Core;

public sealed record ProjectTransaction(
    TransactionId Id,
    string Label,
    ProjectState Before,
    ProjectState After,
    IReadOnlyList<ObjectId> AffectedObjectIds,
    DateTimeOffset CreatedUtc);

public sealed record CandidateApplyResult(bool Applied, bool Conflict, string Message, ProjectState State);

public sealed class ProjectSession
{
    readonly Stack<ProjectTransaction> _undo = new();
    readonly Stack<ProjectTransaction> _redo = new();
    readonly int _historyLimit;
    long _revisionClock;

    public ProjectState Current { get; private set; }
    public long SavedRevisionNumber { get; private set; }
    public bool IsDirty => Current.RevisionNumber != SavedRevisionNumber;
    public IReadOnlyCollection<ProjectTransaction> UndoTransactions => _undo.ToArray();
    public IReadOnlyCollection<ProjectTransaction> RedoTransactions => _redo.ToArray();
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public ProjectSession(ProjectState initial, int historyLimit = 100)
    {
        Current = initial ?? throw new ArgumentNullException(nameof(initial));
        Current.Validate();
        _historyLimit = Math.Clamp(historyLimit, 1, 1000);
        _revisionClock = Current.RevisionNumber;
        SavedRevisionNumber = Current.RevisionNumber;
    }

    public ProjectTransaction Execute(string label, Func<ProjectState, ProjectState> mutation, params ObjectId[] affectedObjectIds)
    {
        if (mutation == null) throw new ArgumentNullException(nameof(mutation));
        var before = Current;
        var after = mutation(before) ?? throw new InvalidOperationException("Project command returned null state.");
        if (after.ProjectId != before.ProjectId) throw new InvalidOperationException("A project command cannot replace project identity.");

        // Revision numbers are durable state identities, not an undo-stack cursor. After undo,
        // a new branch must never reuse a previously observed revision number because that can
        // make a divergent state compare equal to the saved revision and appear clean.
        _revisionClock = checked(_revisionClock + 1);
        after = after.WithRevisionNumber(_revisionClock);
        after.Validate();

        var transaction = new ProjectTransaction(
            TransactionId.New(),
            string.IsNullOrWhiteSpace(label) ? "Edit" : label.Trim(),
            before,
            after,
            affectedObjectIds.Where(x => x.Value != Guid.Empty).Distinct().ToArray(),
            DateTimeOffset.UtcNow);

        Current = after;
        _undo.Push(transaction);
        _redo.Clear();
        TrimUndo();
        return transaction;
    }

    public ProjectTransaction Undo()
    {
        if (_undo.Count == 0) throw new InvalidOperationException("There is no project command to undo.");
        var transaction = _undo.Pop();
        Current = transaction.Before;
        _redo.Push(transaction);
        return transaction;
    }

    public ProjectTransaction Redo()
    {
        if (_redo.Count == 0) throw new InvalidOperationException("There is no project command to redo.");
        var transaction = _redo.Pop();
        Current = transaction.After;
        _undo.Push(transaction);
        TrimUndo();
        return transaction;
    }

    public void MarkSaved()
    {
        SavedRevisionNumber = Current.RevisionNumber;
    }

    /// <summary>
    /// Persists a stable snapshot of the current project state and keeps the in-memory session
    /// consistent with durable storage if the save fails. A successful in-flight save must only
    /// advance the save point to the revision actually persisted; edits made while awaiting I/O
    /// remain dirty instead of being incorrectly treated as durable.
    /// </summary>
    public async Task SaveRecoveringAsync(
        Func<ProjectState, Task> saveAsync,
        Func<Task<ProjectState>> loadLastDurableAsync)
    {
        if (saveAsync == null) throw new ArgumentNullException(nameof(saveAsync));
        if (loadLastDurableAsync == null) throw new ArgumentNullException(nameof(loadLastDurableAsync));

        ProjectState stateToSave = Current;
        ProjectId expectedProjectId = stateToSave.ProjectId;
        try
        {
            await saveAsync(stateToSave);
            SavedRevisionNumber = stateToSave.RevisionNumber;
        }
        catch (Exception saveError)
        {
            try
            {
                ProjectState durable = await loadLastDurableAsync()
                    ?? throw new InvalidDataException("Durable project recovery returned no state.");
                if (durable.ProjectId != expectedProjectId)
                    throw new InvalidDataException("Durable recovery returned a different project identity.");
                ReplaceFromLoad(durable);
            }
            catch (Exception recoveryError)
            {
                throw new AggregateException(
                    "Project save failed and the last durable project state could not be restored into memory.",
                    saveError,
                    recoveryError);
            }

            throw new IOException(
                "Project save failed. The in-memory project was restored to the last durable revision.",
                saveError);
        }
    }

    public void ReplaceFromLoad(ProjectState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        state.Validate();
        Current = state;
        _undo.Clear();
        _redo.Clear();
        _revisionClock = state.RevisionNumber;
        MarkSaved();
    }

    public void ClearHistory(bool markCurrentAsSaved = false)
    {
        _undo.Clear();
        _redo.Clear();
        if (markCurrentAsSaved) MarkSaved();
    }

    public CandidateApplyResult ApplyCandidate(CandidateId candidateId)
    {
        if (!Current.Candidates.TryGetValue(candidateId, out var candidate))
            return new CandidateApplyResult(false, false, "Candidate does not exist.", Current);
        if (!Current.Objects.TryGetValue(candidate.ObjectId, out var obj))
            return new CandidateApplyResult(false, true, "Candidate target object no longer exists.", Current);
        if (candidate.Status is CandidateStatus.Applied or CandidateStatus.Discarded)
            return new CandidateApplyResult(false, false, $"Candidate is already {candidate.Status.ToString().ToLowerInvariant()}.", Current);

        if (obj.ActiveMeshRevisionId != candidate.InputRevisionId)
        {
            Execute("Mark stale AI candidate as conflict", state =>
                state.WithCandidate(candidate with
                {
                    Status = CandidateStatus.Conflict,
                    ConflictReason = $"Object advanced from input revision {candidate.InputRevisionId} to {obj.ActiveMeshRevisionId}."
                }), candidate.ObjectId);
            return new CandidateApplyResult(false, true, "Candidate input is stale. It was preserved as a conflict instead of overwriting newer work.", Current);
        }

        Execute("Apply candidate", state =>
        {
            var updatedObject = obj with { ActiveMeshRevisionId = candidate.OutputRevisionId };
            var updatedCandidate = candidate with { Status = CandidateStatus.Applied, ConflictReason = null };
            return state.WithObject(updatedObject).WithCandidate(updatedCandidate);
        }, candidate.ObjectId);
        return new CandidateApplyResult(true, false, "Candidate applied transactionally.", Current);
    }

    void TrimUndo()
    {
        if (_undo.Count <= _historyLimit) return;
        var keep = _undo.Take(_historyLimit).Reverse().ToArray();
        _undo.Clear();
        foreach (var item in keep) _undo.Push(item);
    }
}
