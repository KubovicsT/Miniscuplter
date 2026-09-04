using System.Diagnostics;

namespace Miniscuplter.Launcher;

internal sealed class LauncherForm : Form
{
    readonly LauncherSettings _settings;
    readonly ModelService _models;
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
    readonly Button _appUpdate = new() { Text = "Update application", Enabled = false };
    readonly Button _launch = new() { Text = "START MINISCULPTER", Height = 46, Dock = DockStyle.Fill };
    readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Visible = false };
    AppUpdateInfo? _latestApp;
    bool _busy;

    public LauncherForm()
    {
        _settings = InstallLayout.Load();
        _models = new ModelService(_settings);
        _updates = new ApplicationUpdateService(_settings);
        Text = "Miniscuplter Launcher v0.9.9";
        Width = 1080; Height = 720; MinimumSize = new Size(850, 560); StartPosition = FormStartPosition.CenterScreen;
        BuildUi();
        Shown += async (_, _) => await RefreshAllAsync(initial: true);
    }

    void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 6 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var title = new Label { Text = "MINISCULPTER", Font = new Font(Font.FontFamily, 20, FontStyle.Bold), AutoSize = true };
        var subtitle = new Label { Text = "Hardware, AI models, updates and launch", AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
        var heading = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        heading.Controls.Add(title); heading.Controls.Add(subtitle); root.Controls.Add(heading);

        var info = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 0, 0, 10) };
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        info.Controls.Add(_hardware, 0, 0); info.Controls.Add(_storage, 1, 0); info.Controls.Add(_appVersion, 0, 1);
        var path = new Label { AutoSize = true, Text = "Install location: " + _settings.InstallRoot };
        info.Controls.Add(path, 1, 1); root.Controls.Add(info);

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });
        _grid.Columns.Add("Model", "Model"); _grid.Columns.Add("Role", "Role"); _grid.Columns.Add("Installed", "Installed");
        _grid.Columns.Add("Size", "Approx. size"); _grid.Columns.Add("Version", "Installed revision"); _grid.Columns.Add("Update", "Update");
        _grid.SelectionChanged += (_, _) => UpdateButtons(); root.Controls.Add(_grid);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 8, 0, 8) };
        _install.Click += async (_, _) => await ModelActionAsync("install");
        _remove.Click += async (_, _) => await ModelActionAsync("remove");
        _updateModel.Click += async (_, _) => await ModelActionAsync("update");
        _refresh.Click += async (_, _) => await RefreshAllAsync(initial: false);
        var openModels = new Button { Text = "Open model folder" }; openModels.Click += (_, _) => OpenFolder(_settings.DataRoot);
        var openInstall = new Button { Text = "Open install folder" }; openInstall.Click += (_, _) => OpenFolder(_settings.InstallRoot);
        buttons.Controls.AddRange(new Control[] { _install, _remove, _updateModel, _refresh, openModels, openInstall }); root.Controls.Add(buttons);

        var updateRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3 };
        updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); updateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        _appUpdate.Click += async (_, _) => await ApplyApplicationUpdateAsync();
        updateRow.Controls.Add(_status, 0, 0); updateRow.Controls.Add(_appUpdate, 1, 0); updateRow.Controls.Add(_progress, 2, 0); root.Controls.Add(updateRow);

        _launch.Font = new Font(Font.FontFamily, 12, FontStyle.Bold); _launch.Click += (_, _) => LaunchApplication(); root.Controls.Add(_launch);
    }

    async Task RefreshAllAsync(bool initial)
    {
        if (_busy) return; SetBusy(true, "Checking hardware and installed AI models…");
        try
        {
            if (!_models.IsAvailable)
            {
                _hardware.Text = "AI runtime: unavailable"; _storage.Text = _models.AvailabilityMessage;
                _grid.Rows.Clear(); _status.Text = "The application can still launch, but local AI management requires the bundled/backend Python runtime.";
            }
            else
            {
                var snapshot = await _models.GetStatusAsync(_settings.CheckModelUpdates);
                _hardware.Text = snapshot.Hardware.CudaAvailable
                    ? $"GPU: {snapshot.Hardware.Gpu ?? "CUDA GPU"} · {snapshot.Hardware.VramMb / 1024.0:0.0} GB VRAM · recommended {snapshot.Hardware.RecommendedProfile}"
                    : $"GPU: no CUDA device detected · recommended {snapshot.Hardware.RecommendedProfile}";
                _storage.Text = $"AI data: {snapshot.DataRoot} · {snapshot.FreeGb:0.0} GB free / {snapshot.TotalGb:0.0} GB";
                PopulateModels(snapshot.Models);
                int updates = snapshot.Models.Count(m => m.Installed && m.UpdateAvailable);
                int missingRecommended = RecommendedFor(snapshot.Hardware.VramMb).Count(id => snapshot.Models.Any(m => m.Id == id && !m.Installed));
                _status.Text = updates > 0
                    ? $"{updates} installed AI model update{(updates == 1 ? "" : "s")} available. Updates are never installed automatically."
                    : missingRecommended > 0 ? $"Hardware check complete. {missingRecommended} recommended model{(missingRecommended == 1 ? " is" : "s are")} not installed." : "Hardware and AI model checks complete.";
            }

            if (_settings.CheckApplicationUpdates)
            {
                try
                {
                    _latestApp = await _updates.CheckAsync();
                    _appVersion.Text = _latestApp.Available
                        ? $"Application: v{_latestApp.CurrentVersion} · v{_latestApp.LatestVersion} available"
                        : $"Application: v{_latestApp.CurrentVersion} · up to date";
                    _appUpdate.Enabled = _latestApp.Available && !string.IsNullOrWhiteSpace(_latestApp.DownloadUrl);
                    if (_latestApp.Available && string.IsNullOrWhiteSpace(_latestApp.DownloadUrl))
                        _status.Text += " The new release has no Windows ZIP update asset, so use its installer/release page.";
                }
                catch (Exception ex)
                {
                    _appVersion.Text = "Application update check unavailable";
                    if (!initial) _status.Text = "Update check failed: " + ex.Message;
                }
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
            int index = _grid.Rows.Add(m.Id, m.Name, m.Kind, m.Installed ? "Yes" : "No", $"~{m.EstimatedGb:0.0} GB",
                ShortRevision(m.InstalledRevision), m.UpdateAvailable ? "AVAILABLE" : m.UpdateError != null ? "Check unavailable" : m.Installed ? "Current" : "—");
            var row = _grid.Rows[index]; row.Tag = m;
            if (m.UpdateAvailable) row.DefaultCellStyle.Font = new Font(_grid.Font, FontStyle.Bold);
            if (m.Id == selected) row.Selected = true;
        }
        if (_grid.SelectedRows.Count == 0 && _grid.Rows.Count > 0) _grid.Rows[0].Selected = true;
    }

    async Task ModelActionAsync(string action)
    {
        if (_busy || _grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not ModelSnapshot model) return;
        if (action == "remove")
        {
            var result = MessageBox.Show(this, $"Delete {model.Name} from the model store?\n\nThis removes its installed files. It does not affect projects.", "Remove AI model", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;
        }
        if (action == "update")
        {
            var result = MessageBox.Show(this, $"A newer upstream revision of {model.Name} is available.\n\nUpdate this model now? Model updates can change output quality or compatibility and are never automatic.", "Update AI model", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
        }
        SetBusy(true, action switch { "install" => $"Installing {model.Name}…", "remove" => $"Removing {model.Name}…", _ => $"Updating {model.Name}…" });
        try
        {
            if (action == "install") await _models.InstallAsync(model.Id);
            else if (action == "remove") await _models.RemoveAsync(model.Id);
            else await _models.UpdateAsync(model.Id);
            _status.Text = $"{model.Name}: {action} completed.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, $"{model.Name} {action} failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetBusy(false); }
        await RefreshAllAsync(initial: false);
    }

    async Task ApplyApplicationUpdateAsync()
    {
        if (_busy || _latestApp is not { Available: true } info) return;
        var result = MessageBox.Show(this, $"Update Miniscuplter from v{info.CurrentVersion} to v{info.LatestVersion}?\n\nThe update is downloaded only after you approve it. AI models, projects, exports and user data are preserved.", "Application update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;
        SetBusy(true, $"Downloading Miniscuplter v{info.LatestVersion}…"); _progress.Visible = true; _progress.Value = 0;
        try
        {
            var progress = new Progress<int>(p => _progress.Value = Math.Clamp(p, 0, 100));
            string package = await _updates.DownloadPackageAsync(info, progress);
            _status.Text = "Update downloaded. Restarting launcher into the staged updater…";
            _updates.StartStagedUpdate(package);
            BeginInvoke(Close);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Application update failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { if (!IsDisposed) { _progress.Visible = false; SetBusy(false); } }
    }

    void LaunchApplication()
    {
        try
        {
            string app = InstallLayout.ResolveApp(_settings);
            if (!File.Exists(app)) { MessageBox.Show(this, $"Miniscuplter executable was not found:\n{app}\n\nRepair or reinstall the application.", "Application missing", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            Process.Start(InstallLayout.CreateAppStartInfo(_settings));
            WindowState = FormWindowState.Minimized;
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not start Miniscuplter", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    void SetBusy(bool busy, string? text = null)
    {
        _busy = busy; _install.Enabled = !busy; _remove.Enabled = !busy; _refresh.Enabled = !busy; _launch.Enabled = !busy;
        if (text != null) _status.Text = text; Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        UpdateButtons();
    }

    void UpdateButtons()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not ModelSnapshot m)
        { _install.Enabled = _remove.Enabled = _updateModel.Enabled = false; return; }
        _install.Enabled = !_busy && !m.Installed; _remove.Enabled = !_busy && m.Installed; _updateModel.Enabled = !_busy && m.Installed && m.UpdateAvailable;
    }

    string? SelectedId() => _grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].Tag is ModelSnapshot m ? m.Id : null;
    static string ShortRevision(string? revision) => string.IsNullOrWhiteSpace(revision) ? "—" : revision.Length > 12 ? revision[..12] : revision;
    static string[] RecommendedFor(int vramMb) => vramMb >= 6144 ? new[] { "sdxl-base", "triposr", "hunyuan21-shape", "clipseg-smart-select" } : new[] { "sd21", "triposr", "clipseg-smart-select" };

    static void OpenFolder(string path)
    {
        try { Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true }); } catch { }
    }
}
