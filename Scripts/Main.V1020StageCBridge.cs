using Godot;
using Miniscuplter.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Miniscuplter;

public partial class Main
{
    readonly ProjectStore _v1020StageCStore = new();
    readonly SemaphoreSlim _v1020StageCGate = new(1, 1);
    ProjectSession? _v1020StageCSession;
    string _v1020StageCProjectPath = "";
    GenerationJobBinding? _v1020GenerationBinding;
    ImageToMeshCandidateState? _v1020PendingCandidate;
    ArrayMesh? _v1020PendingCandidateMesh;
    Button? _v1020ApplyCandidate;
    Button? _v1020DiscardCandidate;
    Label? _v1020CandidateStatus;

    public void InstallV1020StageCBridge()
    {
        var accept = FindChild("2D", true, false) is Node twoD
            ? V1020Descendants(twoD).OfType<Button>().FirstOrDefault(b => b.Text == "Accept Current Image as Baseline")
            : null;
        if (accept != null)
        {
            accept.Pressed -= AcceptV1011Baseline;
            accept.Pressed += V1020AcceptBaseline;
        }

        if (_v109Generate3D != null)
        {
            _v109Generate3D.Pressed -= V109Generate3DAsync;
            _v109Generate3D.Pressed += V1020Generate3DAsync;
        }

        if (_v109Cancel3D != null)
            _v109Cancel3D.Pressed += V1020CancelStageCGeneration;

        if (_v1093DPanel != null)
        {
            _v1020CandidateStatus = new Label
            {
                Text = "3D candidate: none",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            _v1093DPanel.AddChild(_v1020CandidateStatus);
            var row = new HBoxContainer();
            _v1020ApplyCandidate = new Button { Text = "Apply 3D Candidate", Disabled = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            _v1020ApplyCandidate.Pressed += V1020ApplyCandidate;
            _v1020DiscardCandidate = new Button { Text = "Discard Candidate", Disabled = true };
            _v1020DiscardCandidate.Pressed += V1020DiscardCandidate;
            row.AddChild(_v1020ApplyCandidate);
            row.AddChild(_v1020DiscardCandidate);
            _v1093DPanel.AddChild(row);
        }

        _ = V1020RestoreStageCStateAsync();
    }

    static System.Collections.Generic.IEnumerable<Node> V1020Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node nested in V1020Descendants(child)) yield return nested;
        }
    }

    async void V1020AcceptBaseline()
    {
        string candidate = !string.IsNullOrWhiteSpace(_lastEditedImage) && File.Exists(_lastEditedImage) ? _lastEditedImage
            : !string.IsNullOrWhiteSpace(_v03StartingImage) && File.Exists(_v03StartingImage) ? _v03StartingImage
            : !string.IsNullOrWhiteSpace(_lastCapture) && File.Exists(_lastCapture) ? _lastCapture : "";
        if (string.IsNullOrWhiteSpace(candidate))
        {
            if (_v1011BaselineStatus != null) _v1011BaselineStatus.Text = "Baseline: no usable image yet — generate or load one first.";
            SetStatus("Generate a concept or load your own starting image before accepting the baseline.");
            return;
        }

        if (_v109Generate3D != null)
        {
            _v109Generate3D.Disabled = true;
            _v109Generate3D.Text = "Saving Accepted Baseline…";
        }
        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                await V1020EnsureSessionAsync();
                var session = _v1020StageCSession!;
                RevisionId? parent = StageCGeneration.AcceptedBaseline(session.Current);
                var imageRevision = await StageCAssetStore.CreateImageRevisionAsync(
                    _v1020StageCProjectPath, candidate, "3d-baseline", "editor-accepted-baseline", parent);
                session.Execute("Store accepted 2D baseline image revision", state => state.WithImageRevision(imageRevision));
                StageCGeneration.AcceptBaseline(session, imageRevision.Id);
                await V1020SaveSessionAsync();
                _v1011BaselineImage = StageCAssetStore.ResolveImagePath(_v1020StageCProjectPath, imageRevision);
                _lastEditedImage = _v1011BaselineImage;
                if (_v1011BaselineStatus != null)
                    _v1011BaselineStatus.Text = $"Baseline accepted and saved: {Path.GetFileName(candidate)} · revision {imageRevision.Id}";
                if (_v109Generate3D != null)
                {
                    _v109Generate3D.Disabled = false;
                    _v109Generate3D.Text = "Generate 3D from Accepted Baseline";
                }
                SetStatus("2D baseline saved as an immutable project revision. Continue to the 3D tab.");
            }
            finally { _v1020StageCGate.Release(); }
        }
        catch (Exception ex)
        {
            if (_v109Generate3D != null)
            {
                _v109Generate3D.Disabled = true;
                _v109Generate3D.Text = "Accept a 2D Baseline First";
            }
            if (_v1011BaselineStatus != null) _v1011BaselineStatus.Text = "Baseline was not accepted: " + ex.Message;
            SetStatus("Could not durably accept 2D baseline: " + ex.Message);
        }
    }

    async void V1020Generate3DAsync()
    {
        if (_v1093DBusy) return;
        string image = V1011Approved2DSource();
        if (string.IsNullOrWhiteSpace(image) || !File.Exists(image))
        {
            SetV1093DResult("3D status: no durable accepted baseline.", "Accept the current 2D image as the baseline first.");
            return;
        }

        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                await V1020EnsureSessionAsync();
                _v1020GenerationBinding = StageCGeneration.BeginImageToMesh(_v1020StageCSession!.Current);
            }
            finally { _v1020StageCGate.Release(); }
        }
        catch (Exception ex)
        {
            SetV1093DResult("3D status: could not bind generation to project state.", ex.Message);
            return;
        }

        string output = AppDataRoot.Resolve($"ai_part_{DateTime.Now:yyyyMMdd_HHmmss_fff}.stl");
        string prompt = _prompt?.Text.Trim() ?? "";
        _v1093DBusy = true;
        _v1093DStarted = DateTime.UtcNow;
        _v1093DProvider = _v098Routes.Quality3D;
        SetV1093DBusy(true);
        try
        {
            var binding = _v1020GenerationBinding ?? throw new InvalidOperationException("Stage-C generation binding was lost before submission.");
            var transportContext = new AiStageCGenerationContext(
                binding.JobId.ToString(),
                binding.ProjectId.ToString(),
                binding.InputProjectRevisionNumber,
                binding.InputImageRevisionId.ToString(),
                binding.OutputObjectId.ToString());

            SetV1093DPhase("Checking local AI service…", $"Bound to baseline revision {binding.InputImageRevisionId} · job {binding.JobId}.", 5);
            if (!await _ai.HealthAsync())
                throw new InvalidOperationException("The local AI backend did not answer its health check. Use Repair AI Runtime in Launcher.");
            if (_v097ActivePreset != null)
            {
                SetV1093DPhase("Applying quality preset…", $"{_v097ActivePreset.Name} · {_v097ActivePreset.ShapeSteps} 3D steps", 10);
                await PushV097PresetToBackendAsync(_v097ActivePreset);
            }
            SetV1093DPhase("Resolving qualified 3D provider…", "Using readiness-aware Settings → Models routing.", 15);
            if (_v1093DProvider.Equals("auto", StringComparison.OrdinalIgnoreCase))
                _v1093DProvider = await ResolveV109Quality3DProviderAsync();
            SetV1093DPhase($"{_v1093DProvider} loading / reconstructing…", "The result is identity-bound and remains a review candidate until explicitly applied.", 22);

            AiStageC3DResult aiResult = await _ai.Generate3DStageCAsync(
                image, prompt, output, "quality", _v1093DProvider, transportContext);
            string path = aiResult.Path;
            string actualProvider = string.IsNullOrWhiteSpace(aiResult.Provider) ? _v1093DProvider : aiResult.Provider;

            SetV1093DPhase("Validating generated STL…", path, 90);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidOperationException("The 3D provider returned without a usable STL file.");
            var mesh = MeshIO.LoadStl(path);
            if (mesh.GetSurfaceCount() == 0)
                throw new InvalidOperationException("The generated STL contains no renderable mesh surface.");

            SetV1093DPhase("Saving immutable candidate revision…", "The identity-verified mesh is being copied into the transactional project store before review.", 96);
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session was lost.");
                MeshData data = V1013MeshData(mesh);
                var revision = await _v1020StageCStore.CreateMeshRevisionAsync(
                    _v1020StageCProjectPath, binding.OutputObjectId, data, $"image-to-3d:{actualProvider}");
                var registeredCandidate = StageCGeneration.RegisterResult(
                    session, binding, revision, actualProvider, $"verified-backend-stl:{Path.GetFileName(path)};job:{binding.JobId}");
                await V1020SaveSessionAsync();
                _v1020PendingCandidate = registeredCandidate;
                _v1020PendingCandidateMesh = mesh;
                V1020RefreshCandidateControls();
            }
            finally { _v1020StageCGate.Release(); }

            double seconds = (DateTime.UtcNow - _v1093DStarted).TotalSeconds;
            if (_v1020PendingCandidate?.Status == CandidateStatus.Conflict)
                SetV1093DResult($"3D status: completed in {seconds:0}s, preserved as conflict.", _v1020PendingCandidate.ConflictReason ?? "The accepted baseline changed while inference was running.");
            else
                SetV1093DResult($"3D status: identity-verified candidate ready from {actualProvider} in {seconds:0}s.", "Review the candidate, then choose Apply 3D Candidate or Discard Candidate.");
        }
        catch (Exception ex)
        {
            double sec = (DateTime.UtcNow - _v1093DStarted).TotalSeconds;
            string detail = V108FriendlyAiError(ex);
            SetV1093DResult($"3D status: FAILED after {sec:0}s.", detail);
            SetStatus("3D AI error: " + detail);
            V109ShowError("2D → 3D generation failed", detail);
        }
        finally
        {
            _v1020GenerationBinding = null;
            _v1093DBusy = false;
            SetV1093DBusy(false);
            if (_v109Generate3D != null && !string.IsNullOrWhiteSpace(V1011Approved2DSource()))
            {
                _v109Generate3D.Disabled = false;
                _v109Generate3D.Text = "Generate 3D from Accepted Baseline";
            }
        }
    }

    void V1020CancelStageCGeneration()
    {
        if (!_v1093DBusy) return;
        SetStatus("Cancelling Stage-C 3D generation; the accepted baseline and any previously saved candidate remain intact.");
    }

    async void V1020ApplyCandidate()
    {
        if (_v1020PendingCandidate == null) return;
        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                var candidate = _v1020PendingCandidate;
                var result = StageCGeneration.ApplyCandidate(session, candidate.Id, $"AI 3D — {candidate.Provider}");
                if (!result.Applied)
                {
                    var refreshed = StageCGeneration.ReadCandidates(session.Current).FirstOrDefault(x => x.Id == candidate.Id);
                    await V1020SaveSessionAsync();
                    _v1020PendingCandidate = refreshed;
                    V1020RefreshCandidateControls();
                    SetStatus(result.Message);
                    return;
                }
                await V1020SaveSessionAsync();
                ArrayMesh mesh = _v1020PendingCandidateMesh ?? V1020LoadCandidateMesh(candidate);
                AddMeshObject(mesh, $"AI 3D — {candidate.Provider}");
                if (_selected != null) _v1013ObjectIds[_selected.GetInstanceId()] = candidate.OutputObjectId;
                FrameSelected();
                _v1020PendingCandidate = StageCGeneration.ReadCandidates(session.Current).First(x => x.Id == candidate.Id);
                V1020RefreshCandidateControls();
                SetStatus("3D candidate applied transactionally and added to the visible scene.");
            }
            finally { _v1020StageCGate.Release(); }
        }
        catch (Exception ex) { SetStatus("Could not apply 3D candidate: " + ex.Message); }
    }

    async void V1020DiscardCandidate()
    {
        if (_v1020PendingCandidate == null) return;
        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
                var result = StageCGeneration.DiscardCandidate(session, _v1020PendingCandidate.Id);
                await V1020SaveSessionAsync();
                _v1020PendingCandidate = null;
                _v1020PendingCandidateMesh = null;
                V1020RefreshCandidateControls();
                SetStatus(result.Message);
            }
            finally { _v1020StageCGate.Release(); }
        }
        catch (Exception ex) { SetStatus("Could not discard 3D candidate: " + ex.Message); }
    }

    async Task V1020EnsureSessionAsync()
    {
        if (_v1020StageCSession != null) return;
        string projects = AppDataRoot.Resolve("projects");
        Directory.CreateDirectory(projects);
        _v1020StageCProjectPath = Path.Combine(projects, "stagec_working.msculpt2");
        ProjectState state;
        if (File.Exists(_v1020StageCProjectPath))
        {
            var loaded = await _v1020StageCStore.LoadWithRecoveryAsync(_v1020StageCProjectPath);
            state = loaded.State;
            if (loaded.Recovered) SetStatus("Stage-C project state recovered from a verified checkpoint; the damaged primary was left untouched.");
        }
        else
        {
            state = ProjectState.Create("Stage C working project")
                .WithMetadata("compatibility_bridge", "v1.0.20-editor");
            await _v1020StageCStore.SaveAsync(state, _v1020StageCProjectPath);
        }
        _v1020StageCSession = new ProjectSession(state);
        _v1020StageCSession.MarkSaved();
    }

    async Task V1020SaveSessionAsync()
    {
        var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
        await _v1020StageCStore.SaveAsync(session.Current, _v1020StageCProjectPath);
        session.MarkSaved();
    }

    async Task V1020RestoreStageCStateAsync()
    {
        try
        {
            await _v1020StageCGate.WaitAsync();
            try
            {
                await V1020EnsureSessionAsync();
                var session = _v1020StageCSession!;
                RevisionId? baselineId = StageCGeneration.AcceptedBaseline(session.Current);
                if (baselineId is { } id && session.Current.ImageRevisions.TryGetValue(id, out var baseline))
                {
                    string path = StageCAssetStore.ResolveImagePath(_v1020StageCProjectPath, baseline);
                    if (File.Exists(path))
                    {
                        _v1011BaselineImage = path;
                        _lastEditedImage = path;
                        SyncV1015CanvasSource(path);
                        if (_v1011BaselineStatus != null) _v1011BaselineStatus.Text = $"Baseline restored: revision {id}";
                        if (_v109Generate3D != null)
                        {
                            _v109Generate3D.Disabled = false;
                            _v109Generate3D.Text = "Generate 3D from Accepted Baseline";
                        }
                    }
                }

                _v1020PendingCandidate = StageCGeneration.ReadCandidates(session.Current)
                    .Where(x => x.Status is CandidateStatus.Ready or CandidateStatus.Conflict)
                    .OrderByDescending(x => x.CreatedUtc)
                    .FirstOrDefault();
                if (_v1020PendingCandidate != null)
                {
                    try { _v1020PendingCandidateMesh = V1020LoadCandidateMesh(_v1020PendingCandidate); }
                    catch { _v1020PendingCandidateMesh = null; }
                }
                V1020RefreshCandidateControls();
            }
            finally { _v1020StageCGate.Release(); }
        }
        catch (Exception ex)
        {
            if (_v1020CandidateStatus != null) _v1020CandidateStatus.Text = "Stage-C project state unavailable: " + ex.Message;
        }
    }

    ArrayMesh V1020LoadCandidateMesh(ImageToMeshCandidateState candidate)
    {
        var session = _v1020StageCSession ?? throw new InvalidOperationException("Stage-C project session is unavailable.");
        if (!session.Current.MeshRevisions.TryGetValue(candidate.OutputMeshRevisionId, out var revision))
            throw new InvalidDataException("Candidate mesh revision is missing.");
        string path = ProjectStore.ResolveAsset(ProjectLayout.FromManifest(_v1020StageCProjectPath), revision.AssetPath);
        MeshData data = MeshBinaryCodec.Read(path);
        var vertices = new Vector3[data.VertexCount];
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = new Vector3(data.Positions[i * 3], data.Positions[i * 3 + 1], data.Positions[i * 3 + 2]);
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Index] = data.Indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    void V1020RefreshCandidateControls()
    {
        var candidate = _v1020PendingCandidate;
        if (_v1020CandidateStatus != null)
        {
            _v1020CandidateStatus.Text = candidate == null ? "3D candidate: none"
                : candidate.Status == CandidateStatus.Conflict
                    ? "3D candidate: CONFLICT — preserved, not applied. " + (candidate.ConflictReason ?? "Input state changed.")
                    : $"3D candidate: {candidate.Status} · {candidate.Provider} · {candidate.OutputMeshRevisionId}";
        }
        if (_v1020ApplyCandidate != null)
            _v1020ApplyCandidate.Disabled = candidate == null || candidate.Status != CandidateStatus.Ready;
        if (_v1020DiscardCandidate != null)
            _v1020DiscardCandidate.Disabled = candidate == null || candidate.Status is CandidateStatus.Applied or CandidateStatus.Discarded;
    }
}
