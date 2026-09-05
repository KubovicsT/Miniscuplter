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
    readonly Button _cancel = new() { Text = "Cancel", Width = 90 };
    readonly Button _close = new() { Text = "Close", Enabled = false, Width = 90 };
    readonly System.Windows.Forms.Timer _timer = new() { Interval = 900 };
    readonly Stopwatch _elapsed = Stopwatch.StartNew();
    readonly CancellationTokenSource _cts = new();
    bool _running;
    bool _sizing;
    bool _closeAfterCancel;
    long _verifiedPayloadExpectedBytes;

    public bool Succeeded { get; private set; }
    public bool Cancelled { get; private set; }
    public string? FailureMessage { get; private set; }

    public ModelOperationDialog(LauncherSettings settings, ModelSnapshot model, string action)
    {
        _settings = settings; _model = model; _action = action;
        Text = $"{ActionWord(action)} AI model - {model.Name}"; Width = 780; Height = 540; MinimumSize = new Size(640, 420); StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
        BuildUi(); Shown += async (_, _) => await RunAsync(); FormClosing += OnFormClosing; FormClosed += (_, _) => _cts.Dispose();
    }

    void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 6 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); Controls.Add(root);
        _title.Text = $"{ActionWord(_action)} {_model.Name}";
        _status.Text = _action == "remove" ? "Preparing model removal…" : _model.ResumeAvailable ? "Checking interrupted download before resume…" : "Preparing verified model download/install…";
        _detail.Text = _action == "remove" ? "Installed files will be removed transactionally." : $"Audited model payload: ~{_model.EstimatedGb:0.0} GB. Exact upstream bytes are checked before download; provider runtimes can use additional disk space.";
        root.Controls.Add(_title); root.Controls.Add(_status); root.Controls.Add(_detail); root.Controls.Add(_progress); root.Controls.Add(_log);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        _close.Click += (_, _) => Close(); _cancel.Click += (_, _) => RequestCancel(false); _copy.Click += (_, _) => { if (!string.IsNullOrEmpty(_log.Text)) Clipboard.SetText(_log.Text); };
        buttons.Controls.Add(_close); buttons.Controls.Add(_cancel); buttons.Controls.Add(_copy); root.Controls.Add(buttons);
    }

    void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_running) return;
        e.Cancel = true;
        if (_cts.IsCancellationRequested) return;
        if (MessageBox.Show(this, "Cancel this model operation?\n\nDownloaded partial files will be kept. On the next launcher start Miniscuplter will detect them, re-check the upstream revision and manifest, and offer to resume.", "Cancel model operation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            RequestCancel(true);
    }

    void RequestCancel(bool closeWhenStopped)
    {
        if (!_running || _cts.IsCancellationRequested) return;
        _closeAfterCancel |= closeWhenStopped; _cancel.Enabled = false; _status.Text = "Cancelling safely…"; _detail.Text = "Stopping the download. Partial files are being preserved for verified resume.";
        AppendLog($"[{DateTime.Now:HH:mm:ss}] Cancellation requested. Partial stage will be kept for resume."); _cts.Cancel();
    }

    async Task RunAsync()
    {
        _running = true; _timer.Tick += async (_, _) => await UpdateApproximateProgressAsync(); _timer.Start();
        AppendLog($"[{DateTime.Now:HH:mm:ss}] {_title.Text}"); AppendLog($"Data folder: {_settings.DataRoot}");
        if (_model.ResumeAvailable) AppendLog($"Interrupted {_model.ResumeAction ?? _action} detected: ~{_model.StagedGb:0.00} GB staged. Upstream revision and selected files will be verified before reuse.");
        if (_action != "remove") AppendLog($"Audited payload estimate: ~{_model.EstimatedGb:0.0} GB. The exact selected Hugging Face payload is queried before download.");
        try
        {
            var service = new ModelService(_settings); var events = new Progress<ModelOperationEvent>(OnOperationEvent);
            await service.RunModelOperationAsync(_action, _model.Id, events, _cts.Token);
            Succeeded = true; _progress.Style = ProgressBarStyle.Continuous; _progress.Value = 100; _status.Text = $"{_model.Name}: {_action} completed successfully."; _detail.Text = $"Completed and verified in {FormatElapsed(_elapsed.Elapsed)}."; AppendLog($"[{DateTime.Now:HH:mm:ss}] Completed successfully and verified.");
        }
        catch (OperationCanceledException)
        {
            Cancelled = true; _progress.Style = ProgressBarStyle.Marquee; _status.Text = $"{_model.Name}: operation cancelled."; _detail.Text = "Partial download preserved. Restart/refresh the launcher and choose Resume to verify and continue it."; AppendLog($"[{DateTime.Now:HH:mm:ss}] Cancelled. Partial stage preserved for resume.");
        }
        catch (Exception ex)
        {
            FailureMessage = ex.Message; _progress.Style = ProgressBarStyle.Continuous; _progress.Value = 0; _status.Text = $"{_model.Name}: {_action} failed."; _detail.Text = "See the log below. Existing installed models remain untouched; any safe partial download is retained for a verified resume."; AppendLog($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
        }
        finally
        {
            _timer.Stop(); _running = false; _cancel.Enabled = false; _close.Enabled = true; _close.Focus();
            if (_closeAfterCancel && !IsDisposed) BeginInvoke(Close);
        }
    }

    void OnOperationEvent(ModelOperationEvent e)
    {
        string message = e.Message.Trim(); if (string.IsNullOrWhiteSpace(message)) return;
        if (message.StartsWith("MINISCULPTER_EXPECTED_BYTES_ADD=", StringComparison.Ordinal))
        {
            if (long.TryParse(message.AsSpan("MINISCULPTER_EXPECTED_BYTES_ADD=".Length), out long bytes) && bytes > 0)
            {
                _verifiedPayloadExpectedBytes += bytes; double gib = _verifiedPayloadExpectedBytes / 1024d / 1024d / 1024d;
                AppendLog($"[{e.Timestamp.LocalDateTime:HH:mm:ss}] Exact selected upstream payload so far: {gib:0.00} GiB");
            }
            return;
        }
        string prefix = IsActualError(message) ? "ERROR " : IsWarning(message) ? "WARN " : "";
        AppendLog($"[{e.Timestamp.LocalDateTime:HH:mm:ss}] {prefix}{message}");
        if (TryParsePercent(message, out int p)) { _progress.Style = ProgressBarStyle.Continuous; _progress.Value = Math.Clamp(p, 0, 99); _status.Text = $"{ActionWord(_action)} {_model.Name}… {p}%"; }
        else if (message.Contains("Verified upstream payload", StringComparison.OrdinalIgnoreCase)) _status.Text = $"Verified manifest; downloading {_model.Name}…";
        else if (message.Contains("Downloading", StringComparison.OrdinalIgnoreCase) || message.Contains("Fetching", StringComparison.OrdinalIgnoreCase)) _status.Text = $"Downloading {_model.Name}…";
        else if (message.Contains("Installing", StringComparison.OrdinalIgnoreCase) || message.Contains("Building", StringComparison.OrdinalIgnoreCase)) _status.Text = $"Installing provider/runtime for {_model.Name}…";
        else if (message.Contains("Verified downloaded payload", StringComparison.OrdinalIgnoreCase)) _status.Text = $"Verifying and finalizing {_model.Name}…";
    }

    async Task UpdateApproximateProgressAsync()
    {
        if (_sizing || !_running || _action == "remove") return; _sizing = true;
        try
        {
            long bytes = await Task.Run(FindCurrentStagedBytes); if (bytes <= 0) return; double gib = bytes / 1024d / 1024d / 1024d;
            double expected = _verifiedPayloadExpectedBytes > 0 ? _verifiedPayloadExpectedBytes / 1024d / 1024d / 1024d : _model.EstimatedGb;
            if (expected > 0) { int percent = (int)Math.Clamp(Math.Round(gib / expected * 100), 0, 99); _progress.Style = ProgressBarStyle.Continuous; _progress.Value = percent; }
            string exact = _verifiedPayloadExpectedBytes > 0 ? $"exact model payload {_verifiedPayloadExpectedBytes / 1024d / 1024d / 1024d:0.00} GiB" : $"audited model estimate {_model.EstimatedGb:0.0} GB";
            _detail.Text = $"Current staged data: ~{gib:0.00} GB · {exact} · elapsed {FormatElapsed(_elapsed.Elapsed)}. Provider runtimes/source can make staged data larger than model weights.";
        }
        catch { }
        finally { _sizing = false; }
    }

    long FindCurrentStagedBytes()
    {
        string staging = Path.Combine(_settings.DataRoot, ".staging"); if (!Directory.Exists(staging)) return 0;
        DirectoryInfo? candidate = new DirectoryInfo(staging).EnumerateDirectories(_model.Id + "-*", SearchOption.TopDirectoryOnly).OrderByDescending(d => d.LastWriteTimeUtc).FirstOrDefault(); return candidate == null ? 0 : DirectorySize(candidate.FullName);
    }

    static long DirectorySize(string path)
    {
        long total = 0; try { foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)) { try { total += new FileInfo(file).Length; } catch { } } } catch { } return total;
    }

    void AppendLog(string line) { if (IsDisposed) return; _log.AppendText(line + Environment.NewLine); _log.SelectionStart = _log.TextLength; _log.ScrollToCaret(); }
    static bool IsActualError(string message) => message.Contains("Traceback", StringComparison.OrdinalIgnoreCase) || message.Contains("Exception", StringComparison.OrdinalIgnoreCase) || message.Contains("ERROR", StringComparison.OrdinalIgnoreCase) || message.Contains("failed", StringComparison.OrdinalIgnoreCase);
    static bool IsWarning(string message) => message.Contains("warning", StringComparison.OrdinalIgnoreCase) || message.Contains("falling back", StringComparison.OrdinalIgnoreCase) || message.Contains("deprecated", StringComparison.OrdinalIgnoreCase);
    static bool TryParsePercent(string message, out int percent) { var m = Regex.Match(message, @"(?<!\d)(100|\d{1,2})(?:\.\d+)?\s*%"); if (m.Success && double.TryParse(m.Groups[1].Value, out double p)) { percent = (int)Math.Round(p); return true; } percent = 0; return false; }
    static string ActionWord(string action) => action switch { "install" => "Installing", "remove" => "Removing", "update" => "Updating", _ => "Processing" };
    static string FormatElapsed(TimeSpan t) => t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";
}
