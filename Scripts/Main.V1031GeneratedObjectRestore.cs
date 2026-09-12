using Godot;
using Miniscuplter.Core;
using System;
using System.IO;
using System.Linq;

namespace Miniscuplter;

public partial class Main
{
    void V1031RestoreAppliedStageCObjects(ProjectSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ObjectId? lastRestored = null;
        foreach (ImageToMeshCandidateState candidate in StageCGeneration.ReadCandidates(session.Current)
                     .Where(x => x.Status == CandidateStatus.Applied)
                     .OrderBy(x => x.CreatedUtc))
        {
            if (!session.Current.Objects.TryGetValue(candidate.OutputObjectId, out ProjectObject? projectObject))
                throw new InvalidDataException($"Applied generated object {candidate.OutputObjectId} is missing from project state.");
            if (!projectObject.Visible)
                continue;

            MeshInstance3D? presentation = V1020FindSceneObject(projectObject.Id);
            if (presentation == null)
            {
                if (!session.Current.MeshRevisions.TryGetValue(projectObject.ActiveMeshRevisionId, out MeshRevision? revision) ||
                    revision.ObjectId != projectObject.Id)
                    throw new InvalidDataException($"Generated object {projectObject.Id} has no valid active mesh revision.");

                string asset = ProjectStore.ResolveAsset(ProjectLayout.FromManifest(_v1020StageCProjectPath), revision.AssetPath);
                if (!File.Exists(asset))
                    throw new FileNotFoundException("Generated object active mesh asset is missing.", asset);

                ArrayMesh mesh = V1020ArrayMeshFromData(MeshBinaryCodec.Read(asset));
                AddMeshObject(mesh, projectObject.DisplayName);
                presentation = _selected ?? throw new InvalidOperationException("Generated object presentation was not created.");
                _v1013ObjectIds[presentation.GetInstanceId()] = projectObject.Id;
            }

            V1020ProjectObjectStateToScene(presentation, projectObject, reloadMesh: false);
            lastRestored = projectObject.Id;
        }

        if (lastRestored is { } objectId)
        {
            MeshInstance3D? presentation = V1020FindSceneObject(objectId);
            if (presentation != null)
            {
                _v1027ViewportSelection = StageCSelection.BindObject(session.Current, objectId);
                Select(presentation);
                V1017UpdateGizmo();
            }
        }
    }
}
