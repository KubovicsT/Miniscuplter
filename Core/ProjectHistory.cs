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

    public ProjectState Current { get; private set; }
    public IReadOnlyCollection<ProjectTransaction> UndoTransactions => _undo.ToArray();
    public IReadOnlyCollection<ProjectTransaction> RedoTransactions => _redo.ToArray();
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public ProjectSession(ProjectState initial, int historyLimit = 100)
    {
        Current = initial ?? throw new ArgumentNullException(nameof(initial));
        Current.Validate();
        _historyLimit = Math.Clamp(historyLimit, 1, 1000);
    }

    public ProjectTransaction Execute(string label, Func<ProjectState, ProjectState> mutation, params ObjectId[] affectedObjectIds)
    {
        if (mutation == null) throw new ArgumentNullException(nameof(mutation));
        var before = Current;
        var after = mutation(before) ?? throw new InvalidOperationException("Project command returned null state.");
        if (after.ProjectId != before.ProjectId) throw new InvalidOperationException("A project command cannot replace project identity.");
        after = after.WithRevisionNumber(before.RevisionNumber + 1);
        after.Validate();

        var transaction = new ProjectTransaction(
            TransactionId.New(),
            string.IsNullOrWhiteSpace(label) ? "Edit" : label.Trim(),
            before,
            after,
            affectedObjectIds.Distinct().ToArray(),
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
