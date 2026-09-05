using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Miniscuplter.Launcher;

internal sealed class ModelOperationDialog : Form
{
    readonly LauncherSettings _settings;
    readonly ModelSnapshot _model;
    readonly string _action;
    readonly Label _title = new() { AutoSize = true, Font = new Font(SystemFonts.DefaultFont.FontFamily, 12, FontStyle.Bold) };
    readonly Label _status = new() { AutoSize = true };
    readonly Label _detail = new() { AutoSize = true };
    readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 24 };
    readonly RichTextBox _log = new() { Dock = DockStyle.Fill, ReadOnly = true, BackColor = SystemColors.Window, Font = new Font("Consolas", 9), DetectUrls = false };
    readonly Button _copy = new() { Text = "Copy log" };
    readonly Button _close = new() { Text = "Close", Enabled = false, Width = 90 };
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 900 };
    readonly Stopwatch _elapsed = Stopwatch.StartNew();
    bool _running;
    bool _sizing;

    public bool Succeeded { get; private set; }
    public string? FailureMessage { get; private set; }

    public ModelOperationDialog(LauncherSettings settings, ModelSnapshot model, string action)
    {
        _settings = settings;
        _model = model;
        _action = action;
        Text = $"{ActionWord(action)} AI model - {model.Name}";
        Width = 780;
        Height = 540;
        MinimumSize = new Size(640, 420);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BuildUi();
        Shown += async (_, _) => await RunAsync();
        FormClosing += (_, e) => { if (_running) { e.Cancel = true; System.Media.SystemSounds.Beep.Play(); } };
    }

    void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 6 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        _title.Text = $"{ActionWord(_action)} {_model.Name}";
        _status.Text = _action == "remove" ? "Preparing model removal…" : "Preparing model download/install…";
        _detail.Text = _action == "remove" ? "Installed files will be removed transactionally." : $"Approximate installed size: {_model.EstimatedGb:0.0} GB";
        root.Controls.Add(_title);
        root.Controls.Add(_status);
        root.Controls.Add(_detail);
        root.Controls.Add(_progress);
        root.Controls.Add(_log);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        _close.Click += (_, _) => Close();
        _copy.Click += (_, _) => { if (!string.IsNullOrEmpty(_log.Text)) Clipboard.SetText(_log.Text); };
        buttons.Controls.Add(_close);
        buttons.Controls.Add(_copy);
        root.Controls.Add(buttons);
    }

    async Task RunAsync()
    {
        _running = true;
        _timer.Tick += async (_, _) => await UpdateApproximateProgressAsync();
        _timer.Start();
        AppendLog($"[{DateTime.Now:HH:mm:ss}] {_title.Text}");
        AppendLog($"Data folder: {_settings.DataRoot}");
        if (_action != "remove") AppendLog($"Expected final size: ~{_model.EstimatedGb:0.0} GB (progress based on staged bytes is approximate)");

        try
        {
            var service = new ModelService(_settings);
            var events = new Progress<ModelOperationEvent>(OnOperationEvent);
            await service.RunModelOperationAsync(_action, _model.Id, events);
            Succeeded = true;
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = 100;
            _status.Text = $"{_model.Name}: {_action} completed successfully.";
            _detail.Text = $"Completed in {FormatElapsed(_elapsed.Elapsed)}.";
            AppendLog($"[{DateTime.Now:HH:mm:ss}] Completed successfully.");
        }
        catch (Exception ex)
        {
            FailureMessage = ex.Message;
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = 0;
            _status.Text = $"{_model.Name}: {_action} failed.";
            _detail.Text = "See the log below for the error details. The existing installed model, if any, is left untouched by transactional installs/updates.";
            AppendLog($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
        }
        finally
        {
            _timer.Stop();
            _running = false;
            _close.Enabled = true;
            _close.Focus();
        }
    }

    void OnOperationEvent(ModelOperationEvent e)
    {
        string message = e.Message.Trim();
        if (string.IsNullOrWhiteSpace(message)) return;
        AppendLog($"[{e.Timestamp.LocalDateTime:HH:mm:ss}] {(e.Stream == "stderr" ? "ERR " : "")}{message}");
        if (TryParsePercent(message, out int p))
        {
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = Math.Clamp(p, 0, 99);
            _status.Text = $"{ActionWord(_action)} {_model.Name}… {p}%";
        }
        else if (message.Contains("Downloading", StringComparison.OrdinalIgnoreCase) || message.Contains("Fetching", StringComparison.OrdinalIgnoreCase))
            _status.Text = $"Downloading {_model.Name}…";
        else if (message.Contains("Installing", StringComparison.OrdinalIgnoreCase) || message.Contains("Building", StringComparison.OrdinalIgnoreCase))
            _status.Text = $"Installing {_model.Name}…";
    }

    async Task UpdateApproximateProgressAsync()
    {
        if (_sizing || !_running || _action == "remove" || _model.EstimatedGb <= 0) return;
        _sizing = true;
        try
        {
            long bytes = await Task.Run(FindCurrentStagedBytes);
            if (bytes <= 0) return;
            double gib = bytes / 1024d / 1024d / 1024d;
            int percent = (int)Math.Clamp(Math.Round(gib / _model.EstimatedGb * 100), 0, 99);
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = percent;
            _status.Text = $"{ActionWord(_action)} {_model.Name}… approximately {percent}%";
            _detail.Text = $"Staged/downloaded: ~{gib:0.00} GB of ~{_model.EstimatedGb:0.0} GB · elapsed {FormatElapsed(_elapsed.Elapsed)}";
        }
        catch { }
        finally { _sizing = false; }
    }

    long FindCurrentStagedBytes()
    {
        string staging = Path.Combine(_settings.DataRoot, ".staging");
        if (!Directory.Exists(staging)) return 0;
        DirectoryInfo? candidate = new DirectoryInfo(staging).EnumerateDirectories(_model.Id + "-*", SearchOption.TopDirectoryOnly)
            .OrderByDescending(d => d.LastWriteTimeUtc).FirstOrDefault();
        return candidate == null ? 0 : DirectorySize(candidate.FullName);
    }

    static long DirectorySize(string path)
    {
        long total = 0;
        try
        {
            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { total += new FileInfo(file).Length; } catch { }
            }
        }
        catch { }
        return total;
    }

    void AppendLog(string line)
    {
        if (IsDisposed) return;
        _log.AppendText(line + Environment.NewLine);
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    static bool TryParsePercent(string message, out int percent)
    {
        var m = Regex.Match(message, @"(?<!\d)(100|\d{1,2})(?:\.\d+)?\s*%");
        if (m.Success && double.TryParse(m.Groups[1].Value, out double p)) { percent = (int)Math.Round(p); return true; }
        percent = 0; return false;
    }

    static string ActionWord(string action) => action switch { "install" => "Installing", "remove" => "Removing", "update" => "Updating", _ => "Processing" };
    static string FormatElapsed(TimeSpan t) => t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
}
