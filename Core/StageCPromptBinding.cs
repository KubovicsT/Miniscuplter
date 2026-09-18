namespace Miniscuplter.Core;

/// <summary>
/// Stores prompt continuity against the immutable accepted image revision that owns it.
/// Revision-qualified metadata keeps the compatibility manifest shape while preventing a
/// later candidate or baseline from overwriting the prompt lineage of an earlier image.
/// </summary>
public static class StageCPromptBinding
{
    const string PromptKeyPrefix = "stagec.imagePrompt.v1.";
    const string LegacyPromptKey = "ui.last_prompt";

    public static string ReadAcceptedPrompt(ProjectState state, out RevisionId? imageRevisionId)
    {
        ArgumentNullException.ThrowIfNull(state);
        imageRevisionId = StageCGeneration.AcceptedBaseline(state);
        return imageRevisionId is { } id ? ReadPrompt(state, id) : string.Empty;
    }

    public static string ReadPrompt(ProjectState state, RevisionId imageRevisionId)
    {
        ArgumentNullException.ThrowIfNull(state);
        EnsureImageRevision(state, imageRevisionId);
        if (state.Metadata.TryGetValue(Key(imageRevisionId), out string? prompt)) return prompt;
        return StageCGeneration.AcceptedBaseline(state) == imageRevisionId &&
               state.Metadata.TryGetValue(LegacyPromptKey, out string? legacy)
            ? legacy
            : string.Empty;
    }

    public static bool NeedsLegacyMigration(ProjectState state, RevisionId imageRevisionId)
    {
        ArgumentNullException.ThrowIfNull(state);
        EnsureImageRevision(state, imageRevisionId);
        return StageCGeneration.AcceptedBaseline(state) == imageRevisionId &&
               !state.Metadata.ContainsKey(Key(imageRevisionId)) &&
               state.Metadata.ContainsKey(LegacyPromptKey);
    }

    public static ProjectTransaction SetAcceptedPrompt(
        ProjectSession session,
        RevisionId expectedImageRevisionId,
        string prompt)
    {
        ArgumentNullException.ThrowIfNull(session);
        RevisionId current = StageCGeneration.AcceptedBaseline(session.Current)
            ?? throw new InvalidOperationException("Accept a 2D baseline before saving its prompt.");
        if (current != expectedImageRevisionId)
            throw new InvalidOperationException(
                $"Prompt binding is stale: accepted image revision advanced from {expectedImageRevisionId} to {current}.");
        EnsureImageRevision(session.Current, expectedImageRevisionId);
        return session.Execute(
            "Update accepted image prompt",
            state =>
            {
                RevisionId stillCurrent = StageCGeneration.AcceptedBaseline(state)
                    ?? throw new InvalidOperationException("Accepted 2D baseline disappeared while saving its prompt.");
                if (stillCurrent != expectedImageRevisionId)
                    throw new InvalidOperationException("Accepted 2D baseline changed while saving its prompt.");
                EnsureImageRevision(state, expectedImageRevisionId);
                return state
                    .WithMetadata(Key(expectedImageRevisionId), prompt ?? string.Empty)
                    .WithoutMetadata(LegacyPromptKey);
            });
    }

    static string Key(RevisionId imageRevisionId) => PromptKeyPrefix + imageRevisionId;

    static void EnsureImageRevision(ProjectState state, RevisionId imageRevisionId)
    {
        if (imageRevisionId.Value == Guid.Empty || !state.ImageRevisions.ContainsKey(imageRevisionId))
            throw new InvalidOperationException(
                $"Prompt binding references missing image revision {imageRevisionId}.");
    }
}
