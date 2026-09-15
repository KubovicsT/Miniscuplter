using Godot;
using Miniscuplter.Core;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    SelectionBinding? _v1027DurableSmartSelection;
    SelectionId? _v1027FailedSelectionRestore;
    ObjectId? _v1027PendingSmartSelectionClearObjectId;
    bool _v1027SmartSelectionPersisting;
    float[]? _v1027PersistedSmartSelectionValues;
    string _v1027PersistedSmartSelectionQuery = "";

    void V1027ReconcileDurableSmartSelection()
    {
        ProjectSession? session = _v1020StageCSession;
        if (session == null)
            return;

        ObjectId? liveObjectId = null;
        if (_selected != null && GodotObject.IsInstanceValid(_selected) &&
            _v1013ObjectIds.TryGetValue(_selected.GetInstanceId(), out ObjectId selectedObjectId))
            liveObjectId = selectedObjectId;
        else if (_v096SelectionObject != null && GodotObject.IsInstanceValid(_v096SelectionObject) &&
            _v1013ObjectIds.TryGetValue(_v096SelectionObject.GetInstanceId(), out ObjectId selectionObjectId))
            liveObjectId = selectionObjectId;

        if (_v1027DurableSmartSelection is { } existing)
        {
            if (!StageCSelection.IsCurrent(session.Current, existing))
            {
                if (_v096Selection != null && (liveObjectId == null || liveObjectId == existing.ObjectId))
                    ClearV096Selection(false);
                _v1027DurableSmartSelection = null;
                _v1027FailedSelectionRestore = null;
                _v1027PersistedSmartSelectionValues = null;
                _v1027PersistedSmartSelectionQuery = "";
                SetStatus("Smart Selection invalidated because the mesh revision changed; stale vertex indices were not reused.");
            }
            else if (liveObjectId != null && liveObjectId != existing.ObjectId)
            {
                if (_v096Selection != null)
                    ClearV096Selection(false);
                _v1027DurableSmartSelection = null;
                _v1027FailedSelectionRestore = null;
                _v1027PersistedSmartSelectionValues = null;
                _v1027PersistedSmartSelectionQuery = "";
            }
            else
            {
                if (_v096Selection == null)
                    V1027RestoreSmartSelection(existing);
                if (_v096Selection != null &&
                    ReferenceEquals(_v1027PersistedSmartSelectionValues, _v096Selection) &&
                    string.Equals(_v1027PersistedSmartSelectionQuery, _v096SelectionQuery, StringComparison.Ordinal))
                    return;
            }
        }

        if (_v096Selection == null)
        {
            SelectionBinding? restorable = session.Current.Selections.Values
                .Where(binding => binding.Kind == "smart-select-vertex-weights" &&
                                  binding.ObjectId != _v1027PendingSmartSelectionClearObjectId &&
                                  StageCSelection.IsCurrent(session.Current, binding) &&
                                  (liveObjectId == null || binding.ObjectId == liveObjectId))
                .OrderByDescending(binding => binding.CreatedUtc)
                .FirstOrDefault();
            if (restorable != null)
            {
                _v1027DurableSmartSelection = restorable;
                V1027RestoreSmartSelection(restorable);
            }
            return;
        }

        if (_v096SelectionObject == null || !GodotObject.IsInstanceValid(_v096SelectionObject) ||
            !_v1013ObjectIds.TryGetValue(_v096SelectionObject.GetInstanceId(), out ObjectId objectId) ||
            !session.Current.Objects.TryGetValue(objectId, out ProjectObject? obj))
            return;

        if (_v1027DurableSmartSelection is { } currentBinding &&
            currentBinding.ObjectId == objectId &&
            StageCSelection.IsCurrent(session.Current, currentBinding) &&
            ReferenceEquals(_v1027PersistedSmartSelectionValues, _v096Selection) &&
            string.Equals(_v1027PersistedSmartSelectionQuery, _v096SelectionQuery, StringComparison.Ordinal))
            return;

        if (_v1027SmartSelectionPersisting)
            return;

        float[] snapshot = (float[])_v096Selection.Clone();
        string query = _v096SelectionQuery;
        _v1027SmartSelectionPersisting = true;
        _ = V1027PersistSmartSelectionAsync(objectId, obj.ActiveMeshRevisionId, snapshot, query);
    }

    void V1027BeginDurableSmartSelectionClear()
    {
        if (_v1027DurableSmartSelection is not { } binding || _v1020StageCSession == null)
            return;

        _v1027PendingSmartSelectionClearObjectId = binding.ObjectId;
        _v1027DurableSmartSelection = null;
        _v1027FailedSelectionRestore = null;
        _v1027PersistedSmartSelectionValues = null;
        _v1027PersistedSmartSelectionQuery = "";
        _ = V1027ClearDurableSmartSelectionAsync(binding.ObjectId);
    }

    async Task V1027ClearDurableSmartSelectionAsync(ObjectId objectId)
    {
        bool cleared = false;
        string? cleanupWarning = null;
        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                ProjectSession currentSession = _v1020StageCSession ??
                    throw new InvalidOperationException("Stage-C project session is unavailable.");
                string[] retiredAssetPaths = currentSession.Current.Selections.Values
                    .Where(binding => binding.ObjectId == objectId && binding.Kind == "smart-select-vertex-weights")
                    .Select(binding => binding.DataAssetPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                cleared = StageCSelection.RemoveRevisionSelections(currentSession, objectId, "smart-select-vertex-weights") > 0;
                if (cleared)
                {
                    await V1020SaveSessionAsync();
                    ProjectLayout layout = ProjectLayout.FromManifest(_v1020StageCProjectPath);
                    foreach (string relativeAssetPath in retiredAssetPaths)
                    {
                        bool stillReferenced = currentSession.Current.Selections.Values.Any(binding => string.Equals(binding.DataAssetPath, relativeAssetPath, StringComparison.OrdinalIgnoreCase));
                        if (stillReferenced) continue;
                        try { string assetPath = ProjectStore.ResolveAsset(layout, relativeAssetPath); if (File.Exists(assetPath)) File.Delete(assetPath); }
                        catch (Exception cleanupError) { cleanupWarning = cleanupError.Message; }
                    }
                }
            }
            finally { _v1020StageCGate.Release(); }
            if (cleared && cleanupWarning != null) SetStatus("Smart Selection cleared durably, but retired selection asset cleanup failed: " + cleanupWarning);
        }
        catch (Exception ex)
        {
            cleared = false;
            SetStatus("Smart Selection was cleared locally but its durable binding could not be removed: " + ex.Message);
        }
        finally
        {
            _v1027PendingSmartSelectionClearObjectId = null;
            if (!cleared) V1027ReconcileDurableSmartSelection();
        }
    }

    void V1027RestoreSmartSelection(SelectionBinding binding)
    {
        if (_v1027FailedSelectionRestore == binding.Id || _v1020StageCSession == null || !StageCSelection.IsCurrent(_v1020StageCSession.Current, binding)) return;
        MeshInstance3D? target = V1020FindSceneObject(binding.ObjectId);
        if (target?.Mesh is not ArrayMesh mesh) return;
        try
        {
            string asset = ProjectStore.ResolveAsset(ProjectLayout.FromManifest(_v1020StageCProjectPath), binding.DataAssetPath);
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(asset));
            JsonElement root = document.RootElement;
            if (!root.TryGetProperty("weights", out JsonElement weightsElement) || weightsElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Smart Selection asset does not contain vertex weights.");
            float[] weights = weightsElement.EnumerateArray().Select(value => value.GetSingle()).ToArray();
            var meshData = new MeshDataTool();
            if (meshData.CreateFromSurface(mesh, 0) != Error.Ok || meshData.GetVertexCount() != weights.Length) throw new InvalidDataException("Smart Selection asset does not match the current mesh topology.");
            string query = root.TryGetProperty("query", out JsonElement queryElement) ? queryElement.GetString() ?? "" : "";
            _v096Selection = weights; _v096SelectionObject = target; _v096SelectionQuery = query; _v096SelectionTopology = V096MeshSignature(mesh);
            _v1027DurableSmartSelection = binding; _v1027PersistedSmartSelectionValues = _v096Selection; _v1027PersistedSmartSelectionQuery = query; _v1027FailedSelectionRestore = null;
            V096RestoreSelectionView(); RebuildV096SelectionOverlay(); ApplyV096SelectionToSculptMask();
            SetStatus("Revision-bound Smart Selection restored from project history.");
        }
        catch (Exception ex) { _v1027FailedSelectionRestore = binding.Id; SetStatus("Stored Smart Selection could not be restored safely: " + ex.Message); }
    }

    async Task V1027PersistSmartSelectionAsync(ObjectId objectId, RevisionId meshRevisionId, float[] weights, string query)
    {
        SelectionId selectionId = SelectionId.New(); ProjectLayout layout = ProjectLayout.FromManifest(_v1020StageCProjectPath);
        string relative = $"data/selection_{selectionId}.json"; string destination = ProjectStore.ResolveAsset(layout, relative); string temp = destination + ".tmp"; bool bindingCommitted = false;
        try
        {
            layout.EnsureDirectories(); string json = JsonSerializer.Serialize(new { query, weights }); await File.WriteAllTextAsync(temp, json); File.Move(temp, destination, overwrite: true);
            await _v1020StageCGate.WaitAsync();
            try
            {
                ProjectSession currentSession = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                if (!currentSession.Current.Objects.TryGetValue(objectId, out ProjectObject? current) || current.ActiveMeshRevisionId != meshRevisionId) throw new InvalidOperationException("The mesh revision advanced before Smart Selection could be persisted.");
                if (_v096SelectionObject == null || !GodotObject.IsInstanceValid(_v096SelectionObject) || !_v1013ObjectIds.TryGetValue(_v096SelectionObject.GetInstanceId(), out ObjectId liveObjectId) || liveObjectId != objectId) throw new InvalidOperationException("The Smart Selection target changed before persistence completed.");
                SelectionBinding binding = StageCSelection.BindRevisionSelection(currentSession, selectionId, objectId, meshRevisionId, "smart-select-vertex-weights", relative); bindingCommitted = true; await V1020SaveSessionAsync();
                _v1027DurableSmartSelection = binding; _v1027FailedSelectionRestore = null;
                if (_v096Selection != null && _v096Selection.AsSpan().SequenceEqual(weights) && string.Equals(_v096SelectionQuery, query, StringComparison.Ordinal)) { _v1027PersistedSmartSelectionValues = _v096Selection; _v1027PersistedSmartSelectionQuery = _v096SelectionQuery; }
                else { _v1027PersistedSmartSelectionValues = null; _v1027PersistedSmartSelectionQuery = ""; }
            }
            finally { _v1020StageCGate.Release(); }
        }
        catch (Exception ex)
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            try { bool destinationStillReferenced = _v1020StageCSession != null && _v1020StageCSession.Current.Selections.Values.Any(binding => string.Equals(binding.DataAssetPath, relative, StringComparison.OrdinalIgnoreCase)); if (!destinationStillReferenced && File.Exists(destination)) File.Delete(destination); } catch { }
            if (_v1020StageCSession != null && _v1020StageCSession.Current.Objects.TryGetValue(objectId, out ProjectObject? current) && current.ActiveMeshRevisionId != meshRevisionId) { ClearV096Selection(false); SetStatus("Smart Selection invalidated because its mesh revision became stale: " + ex.Message); }
            else SetStatus("Smart Selection remains visible but could not be made durable: " + ex.Message);
        }
        finally { _v1027SmartSelectionPersisting = false; if (_v096Selection != null) V1027ReconcileDurableSmartSelection(); }
    }
}