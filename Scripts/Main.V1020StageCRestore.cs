using Godot;
using Miniscuplter.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    public void InstallV1020AppliedObjectRestore()
    {
        _ = V1020RestoreAppliedObjectsAsync();
    }

    async Task V1020RestoreAppliedObjectsAsync()
    {
        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                await V1020EnsureSessionAsync();
                var session = _v1020StageCSession!;
                var applied = StageCGeneration.ReadCandidates(session.Current)
                    .Where(candidate => candidate.Status == CandidateStatus.Applied)
                    .OrderBy(candidate => candidate.CreatedUtc)
                    .ToArray();

                foreach (var candidate in applied)
                {
                    if (!session.Current.Objects.TryGetValue(candidate.OutputObjectId, out ProjectObject? durableObject))
                        continue;
                    if (_v1013ObjectIds.Values.Contains(candidate.OutputObjectId))
                        continue;

                    ArrayMesh mesh = V1020LoadCandidateMesh(candidate);
                    AddMeshObject(mesh, $"AI 3D — {candidate.Provider}");
                    if (_selected != null)
                    {
                        _v1013ObjectIds[_selected.GetInstanceId()] = candidate.OutputObjectId;
                        V1020ProjectObjectStateToScene(_selected, durableObject);
                    }
                }

                if (applied.Length > 0 && _selected != null)
                    FrameSelected();
            }
            finally { _v1020StageCGate.Release(); }
        }
        catch (Exception ex)
        {
            SetStatus("Stage-C applied-object restore failed: " + ex.Message);
        }
    }
}
