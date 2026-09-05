using System.Diagnostics;

namespace Miniscuplter.Launcher;

internal sealed class LauncherForm : Form
{
    readonly LauncherSettings _settings;
    readonly ApplicationUpdateService _updates;
    readonly Label _hardware = new() { AutoSize = true };
    readonly Label _storage = new() { AutoSize = true };
    readonly Label _appVersion = new() { AutoSize = true };
    readonly Label _status = new() { AutoSize = true };
    readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    readonly Button _install = new() { Text = "Install selected" };
    readonly Button _remove = new() { Text = "Remove selected" };
    readonly Button _updateModel = new() { Text = "Update selected", Enabled = false };
    readonly Button _refresh = new() { Text = "Refresh checks" };
    readonly Button _repairRuntime = new() { Text = "Repair AI runtime" };
    readonly Button _appUpdate = new() { Text = "Update application", Enabled = false };
    readonly Button _launch = new() { Text = "START MINISCULPTER", Height = 46, Dock = DockStyle.Fill };
    readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Visible = false };
    AppUpdateInfo? _latestApp;
    bool _busy;

    public LauncherForm()
    {
        _settings = InstallLayout.Load(); _updates = new ApplicationUpdateService(_settings);
        Text = "Miniscuplter Launcher v1.0.5"; Width = 1080; Height = 720; MinimumSize = new Size(850, 560); StartPosition = FormStartPosition.CenterScreen;
        BuildUi(); Shown += async (_, _) => await RefreshAllAsync(initial: true);
    }

    void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 6 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); Controls.Add(root);
        var heading = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        heading.Controls.Add(new Label { Text = "MINISCULPTER", Font = new Font(Font.FontFamily, 20, FontStyle.Bold), AutoSize = true }); heading.Controls.Add(new Label { Text = "Hardware, AI models, updates and launch", AutoSize = true, Margin = new Padding(0, 0, 0, 10) }); root.Controls.Add(heading);
        var info = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 0, 0, 10) };
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); info.Controls.Add(_hardware, 0, 0); info.Controls.Add(_storage, 1, 0); info.Controls.Add(_appVersion, 0, 1); info.Controls.Add(new Label { AutoSize = true, Text = "Install location: " + _settings.InstallRoot }, 1, 1); root.Controls.Add(info);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false }); _grid.Columns.Add("Model", "Model"); _grid.Columns.Add("Role", "Role"); _grid.Columns.Add("Installed", "Installed"); _grid.Columns.Add("Size", "Audited model payload"); _grid.Columns.Add("Version", "Installed revision"); _grid.Columns.Add("Update", "Status / update"); _grid.SelectionChanged += (_, _) => UpdateButtons(); root.Controls.Add(_grid);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 8, 0, 8) };
        _install.Click += async (_, _) => await ModelActionAsync("install"); _remove.Click += async (_, _) => await ModelActionAsync("remove"); _updateModel.Click += async (_, _) => await ModelActionAsync("update"); _refresh.Click += async (_, _) => await RefreshAllAsync(false); _repairRuntime.Click += async (_, _) => await RepairRuntimeAsync();
        var openModels = new Button { Text = "Open model folder" }; openModels.Click += (_, _) => OpenFolder(_settings.DataRoot); var openInstall = new Button { Text = "Open install folder" }; openInstall.Click += (_, _) => OpenFolder(_settings.InstallRoot); buttons.Controls.AddRange(new Control[] { _install, _remove, _updateModel, _refresh, _repairRuntime, openModels, openInstall }); root.Controls.Add(buttons);
        var updateRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3 }; updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); _appUpdate.Click += async (_, _) => await ApplyApplicationUpdateAsync(); updateRow.Controls.Add(_status, 0, 0); updateRow.Controls.Add(_appUpdate, 1, 0); updateRow.Controls.Add(_progress, 2, 0); root.Controls.Add(updateRow);
        _launch.Font = new Font(Font.FontFamily, 12, FontStyle.Bold); _launch.Click += (_, _) => LaunchApplication(); root.Controls.Add(_launch);
    }

    async Task RefreshAllAsync(bool initial)
    {
        if (_busy) return; SetBusy(true, "Checking hardware, installed models and interrupted downloads…");
        try
        {
            LauncherHardware native = HardwareProbe.Detect();
            _hardware.Text = $"CPU: {native.Cpu} ({native.LogicalProcessors} threads) · RAM {native.RamGb:0.0} GB\nGPU: {(native.Gpu ?? "No NVIDIA CUDA GPU detected")}" + (native.VramMb > 0 ? $" · {native.VramMb / 1024.0:0.0} GB VRAM" : "") + $" · recommended {native.RecommendedPreset}";
            var modelService = new ModelService(_settings);
            if (!modelService.IsAvailable)
            {
                _storage.Text = modelService.AvailabilityMessage; _grid.Rows.Clear(); _status.Text = "Hardware check complete. Local AI management needs its Python environment; click Repair AI Runtime. Setup progress will be shown in a dedicated window. Existing model weights and partial downloads are preserved.";
            }
            else
            {
                var snapshot = await modelService.GetStatusAsync(_settings.CheckModelUpdates); _storage.Text = $"AI data: {snapshot.DataRoot} · {snapshot.FreeGb:0.0} GB free / {snapshot.TotalGb:0.0} GB"; PopulateModels(snapshot.Models);
                var resumable = snapshot.Models.Where(m => m.ResumeAvailable).ToList(); int updates = snapshot.Models.Count(m => m.Installed && m.UpdateAvailable); int missing = RecommendedFor(native.VramMb).Count(id => snapshot.Models.Any(m => m.Id == id && !m.Installed));
                _status.Text = resumable.Count > 0 ? $"{resumable.Count} interrupted model operation{(resumable.Count == 1 ? " is" : "s are")} ready to resume. Select the highlighted model; Miniscuplter will verify the upstream revision and exact file manifest before continuing."
                    : updates > 0 ? $"{updates} installed AI model update{(updates == 1 ? "" : "s")} available. Updates are never installed automatically."
                    : missing > 0 ? $"Hardware check complete. {missing} recommended model{(missing == 1 ? " is" : "s are")} not installed." : "Hardware and AI model checks complete.";
            }
            if (_settings.CheckApplicationUpdates)
            {
                try { _latestApp = await _updates.CheckAsync(); _appVersion.Text = _latestApp.Available ? $"Application: v{_latestApp.CurrentVersion} · v{_latestApp.LatestVersion} available" : $"Application: v{_latestApp.CurrentVersion} · up to date"; _appUpdate.Enabled = _latestApp.Available && !string.IsNullOrWhiteSpace(_latestApp.DownloadUrl); if (_latestApp.Available && string.IsNullOrWhiteSpace(_latestApp.DownloadUrl)) _status.Text += " The release has no Windows ZIP update asset, so use its installer/release page."; }
                catch (Exception ex) { _appVersion.Text = "Application update check unavailable"; if (!initial) _status.Text = "Update check failed: " + ex.Message; }
            }
            else _appVersion.Text = "Application update checks disabled";
        }
        catch (Exception ex) { _status.Text = "Check failed: " + ex.Message; }
        finally { SetBusy(false); UpdateButtons(); }
    }

    void PopulateModels(IEnumerable<ModelSnapshot> models)
    {
        string? selected = SelectedId(); _grid.Rows.Clear();
        foreach (var m in models)
        {
            string status = m.ResumeAvailable ? $"RESUME {m.ResumeAction?.ToUpperInvariant() ?? "INSTALL"} · {m.StagedGb:0.00} GB staged" : m.UpdateAvailable ? "AVAILABLE" : m.UpdateError != null ? "Check unavailable" : m.Installed ? "Current" : "—";
            int index = _grid.Rows.Add(m.Id, m.Name, m.Kind, m.Installed ? "Yes" : m.ResumeAvailable ? "Partial" : "No", m.EstimatedGb > 0 ? $"~{m.EstimatedGb:0.0} GB" : "External runtime", ShortRevision(m.InstalledRevision), status);
            var row = _grid.Rows[index]; row.Tag = m; if (m.UpdateAvailable || m.ResumeAvailable) row.DefaultCellStyle.Font = new Font(_grid.Font, FontStyle.Bold); if (m.Id == selected) row.Selected = true;
        }
        if (_grid.SelectedRows.Count == 0 && _grid.Rows.Count > 0) _grid.Rows[0].Selected = true;
    }

    async Task ModelActionAsync(string action)
    {
        if (_busy || _grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not ModelSnapshot model) return;
        if (action == "remove" && MessageBox.Show(this, $"Delete {model.Name} from the model store?\n\nThis removes its installed files. It does not affect projects.", "Remove AI model", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        if (action == "update" && !model.ResumeAvailable && MessageBox.Show(this, $"A newer upstream revision of {model.Name} is available.\n\nUpdate this model now? Model updates can change output quality or compatibility and are never automatic.", "Update AI model", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        SetBusy(true, model.ResumeAvailable ? $"Resuming {model.Name}…" : action switch { "install" => $"Installing {model.Name}…", "remove" => $"Removing {model.Name}…", _ => $"Updating {model.Name}…" });
        try
        {
            using var dialog = new ModelOperationDialog(_settings, model, action); dialog.ShowDialog(this);
            _status.Text = dialog.Succeeded ? $"{model.Name}: {action} completed and verified." : dialog.Cancelled ? $"{model.Name}: cancelled. Partial files were preserved and can be resumed." : $"{model.Name}: {action} failed. Safe partial files were preserved where possible; refresh to see resume status.";
        }
        finally { SetBusy(false); }
        await RefreshAllAsync(false);
    }

    async Task RepairRuntimeAsync()
    {
        if (_busy || MessageBox.Show(this, "Repair/install the local AI Python environment now?\n\nA setup progress window will show package and download progress. Model weights are not downloaded by runtime repair. Existing model weights and interrupted downloads are preserved.", "Repair AI runtime", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        SetBusy(true, "Preparing local AI runtime setup…");
        try
        {
            using var dialog = new RuntimeSetupDialog(_settings); dialog.ShowDialog(this);
            if (dialog.Succeeded) _status.Text = "AI runtime repaired successfully. Xet/model download support and runtime dependencies were verified.";
            else if (dialog.Cancelled) _status.Text = "AI runtime setup cancelled. Cached runtime downloads were preserved for the next Repair attempt.";
            else
            {
                _status.Text = "AI runtime setup failed. Cached downloads were preserved for retry.";
                if (!string.IsNullOrWhiteSpace(dialog.FailureMessage)) MessageBox.Show(this, dialog.FailureMessage, "AI runtime setup failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally { SetBusy(false); }
        await RefreshAllAsync(false);
    }

    async Task ApplyApplicationUpdateAsync()
    {
        if (_busy || _latestApp is not { Available: true } info) return;
        if (_updates.IsMainApplicationRunning()) { MessageBox.Show(this, "Close the Miniscuplter editor before updating the application. Projects and models are not affected.", "Miniscuplter is running", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (MessageBox.Show(this, $"Update Miniscuplter from v{info.CurrentVersion} to v{info.LatestVersion}?\n\nThe update downloads only after you approve it. AI models, projects, exports and user data are preserved.", "Application update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        SetBusy(true, $"Downloading Miniscuplter v{info.LatestVersion}…"); _progress.Visible = true; _progress.Value = 0;
        try { var progress = new Progress<int>(p => _progress.Value = Math.Clamp(p, 0, 100)); string package = await _updates.DownloadPackageAsync(info, progress); _status.Text = "Update downloaded. Starting staged updater…"; _updates.StartStagedUpdate(package); BeginInvoke(Close); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Application update failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { if (!IsDisposed) { _progress.Visible = false; SetBusy(false); } }
    }

    void LaunchApplication()
    {
        try { string app = InstallLayout.ResolveApp(_settings); if (!File.Exists(app)) { MessageBox.Show(this, $"Miniscuplter executable was not found:\n{app}\n\nRepair or reinstall the application.", "Application missing", MessageBoxButtons.OK, MessageBoxIcon.Error); return; } Process.Start(InstallLayout.CreateAppStartInfo(_settings)); WindowState = FormWindowState.Minimized; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not start Miniscuplter", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    void SetBusy(bool busy, string? text = null) { _busy = busy; _install.Enabled = !busy; _remove.Enabled = !busy; _refresh.Enabled = !busy; _repairRuntime.Enabled = !busy; _launch.Enabled = !busy; if (text != null) _status.Text = text; Cursor = busy ? Cursors.WaitCursor : Cursors.Default; UpdateButtons(); }
    void UpdateButtons()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not ModelSnapshot m) { _install.Enabled = _remove.Enabled = _updateModel.Enabled = false; return; }
        bool resumeInstall = m.ResumeAvailable && !m.Installed && !string.Equals(m.ResumeAction, "update", StringComparison.OrdinalIgnoreCase);
        bool resumeUpdate = m.ResumeAvailable && m.Installed && string.Equals(m.ResumeAction, "update", StringComparison.OrdinalIgnoreCase);
        _install.Text = resumeInstall ? "Resume selected" : "Install selected"; _updateModel.Text = resumeUpdate ? "Resume update" : "Update selected";
        _install.Enabled = !_busy && !m.Installed; _remove.Enabled = !_busy && m.Installed; _updateModel.Enabled = !_busy && m.Installed && (m.UpdateAvailable || resumeUpdate);
    }
    string? SelectedId() => _grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].Tag is ModelSnapshot m ? m.Id : null;
    static string ShortRevision(string? revision) => string.IsNullOrWhiteSpace(revision) ? "—" : revision.Length > 24 ? revision[..24] : revision;
    static string[] RecommendedFor(int vramMb) => vramMb >= 12288 ? new[] { "qwen-image-2512", "hunyuan21-shape", "partcrafter", "clipseg-smart-select" } : vramMb >= 6144 ? new[] { "sdxl-base", "sf3d", "hunyuan2mini", "clipseg-smart-select" } : new[] { "sd21", "triposr", "clipseg-smart-select" };
    static void OpenFolder(string path) { try { Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true }); } catch { } }
}