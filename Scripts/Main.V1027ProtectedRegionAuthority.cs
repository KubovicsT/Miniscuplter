using Godot;
using Miniscuplter.Core;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    SelectionBinding? _v1027DurableSmartSelection;
    bool _v1027SmartSelectionPersisting;
    float[]? _v1027PersistedSmartSelectionValues;
    string _v1027PersistedSmartSelectionQuery = "";

    void V1027ReconcileDurableSmartSelection()
    {
        if (_v1020StageCSession == null || _v096Selection == null ||
            _v096SelectionObject == null || !GodotObject.IsInstanceValid(_v096SelectionObject) ||
            !_v1013ObjectIds.TryGetValue(_v096SelectionObject.GetInstanceId(), out ObjectId objectId) ||
            !_v1020StageCSession.Current.Objects.TryGetValue(objectId, out ProjectObject? obj))
        {
            _v1027DurableSmartSelection = null;
            _v1027PersistedSmartSelectionValues = null;
            _v1027PersistedSmartSelectionQuery = "";
            return;
        }

        if (_v1027DurableSmartSelection is { } existing && existing.ObjectId == objectId)
        {
            if (!StageCSelection.IsCurrent(_v1020StageCSession.Current, existing))
            {
                ClearV096Selection(false);
                _v1027DurableSmartSelection = null;
                _v1027PersistedSmartSelectionValues = null;
                _v1027PersistedSmartSelectionQuery = "";
                SetStatus("Smart Selection invalidated because the mesh revision changed; stale vertex indices were not reused.");
                return;
            }

            if (ReferenceEquals(_v1027PersistedSmartSelectionValues, _v096Selection) &&
                string.Equals(_v1027PersistedSmartSelectionQuery, _v096SelectionQuery, StringComparison.Ordinal))
                return;
        }

        if (_v1027SmartSelectionPersisting)
            return;

        float[] snapshot = (float[])_v096Selection.Clone();
        string query = _v096SelectionQuery;
        _v1027SmartSelectionPersisting = true;
        _ = V1027PersistSmartSelectionAsync(objectId, obj.ActiveMeshRevisionId, snapshot, query);
    }

    async Task V1027PersistSmartSelectionAsync(
        ObjectId objectId,
        RevisionId meshRevisionId,
        float[] weights,
        string query)
    {
        SelectionId selectionId = SelectionId.New();
        ProjectLayout layout = ProjectLayout.FromManifest(_v1020StageCProjectPath);
        string relative = $"data/selection_{selectionId}.json";
        string destination = ProjectStore.ResolveAsset(layout, relative);
        string temp = destination + ".tmp";

        try
        {
            layout.EnsureDirectories();
            string json = JsonSerializer.Serialize(new { query, weights });
            await File.WriteAllTextAsync(temp, json);
            File.Move(temp, destination, overwrite: true);

            await _v1020StageCGate.WaitAsync();
            try
            {
                ProjectSession session = _v1020StageCSession ??
                    throw new InvalidOperationException("Stage-C project session is unavailable.");
                if (!session.Current.Objects.TryGetValue(objectId, out ProjectObject? current) ||
                    current.ActiveMeshRevisionId != meshRevisionId)
                    throw new InvalidOperationException("The mesh revision advanced before Smart Selection could be persisted.");
                if (_v096SelectionObject == null || !GodotObject.IsInstanceValid(_v096SelectionObject) ||
                    !_v1013ObjectIds.TryGetValue(_v096SelectionObject.GetInstanceId(), out ObjectId liveObjectId) ||
                    liveObjectId != objectId)
                    throw new InvalidOperationException("The Smart Selection target changed before persistence completed.");

                SelectionBinding binding = StageCSelection.BindRevisionSelection(
                    session,
                    selectionId,
                    objectId,
                    meshRevisionId,
                    "smart-select-vertex-weights",
                    relative);
                await V1020SaveSessionAsync();
                _v1027DurableSmartSelection = binding;
                _v1027PersistedSmartSelectionValues = _v096Selection;
                _v1027PersistedSmartSelectionQuery = _v096SelectionQuery;
            }
            finally
            {
                _v1020StageCGate.Release();
            }
        }
        catch (Exception ex)
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            if (_v1020StageCSession != null &&
                _v1020StageCSession.Current.Objects.TryGetValue(objectId, out ProjectObject? current) &&
                current.ActiveMeshRevisionId != meshRevisionId)
            {
                ClearV096Selection(false);
                SetStatus("Smart Selection invalidated because its mesh revision became stale: " + ex.Message);
            }
            else
            {
                SetStatus("Smart Selection remains visible but could not be made durable: " + ex.Message);
            }
        }
        finally
        {
            _v1027SmartSelectionPersisting = false;
        }
    }
}
