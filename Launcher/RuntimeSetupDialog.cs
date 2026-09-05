using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Miniscuplter.Launcher;

internal sealed class RuntimeSetupDialog : Form
{
    readonly LauncherSettings _settings;
    readonly Label _title = new() { AutoSize = true, Font = new Font(SystemFonts.DefaultFont.FontFamily, 12, FontStyle.Bold) };
    readonly Label _status = new() { AutoSize = true };
    readonly Label _detail = new() { AutoSize = true };
    readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 24 };
    readonly RichTextBox _log = new() { Dock = DockStyle.Fill, ReadOnly = true, BackColor = SystemColors.Window, Font = new Font("Consolas", 9), DetectUrls = false };
    readonly Button _copy = new() { Text = "Copy log" };
    readonly Button _cancel = new() { Text = "Cancel", Width = 90 };
    readonly Button _close = new() { Text = "Close", Enabled = false, Width = 90 };
    readonly CancellationTokenSource _cts = new();
    readonly Stopwatch _elapsed = Stopwatch.StartNew();
    bool _running;
    bool _closeAfterCancel;

    public bool Succeeded { get; private set; }
    public bool Cancelled { get; private set; }
    public string? FailureMessage { get; private set; }

    public RuntimeSetupDialog(LauncherSettings settings)
    {
        _settings = settings;
        Text = "Repair AI runtime";
        Width = 800;
        Height = 560;
        MinimumSize = new Size(660, 440);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BuildUi();
        Shown += async (_, _) => await RunAsync();
        FormClosing += OnFormClosing;
        FormClosed += (_, _) => _cts.Dispose();
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

        _title.Text = "Repair / install local AI runtime";
        _status.Text = "Preparing Python environment…";
        _detail.Text = "Package and large runtime downloads are shown below. Cached downloads are retained if setup is cancelled or interrupted.";
        root.Controls.Add(_title);
        root.Controls.Add(_status);
        root.Controls.Add(_detail);
        root.Controls.Add(_progress);
        root.Controls.Add(_log);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        _close.Click += (_, _) => Close();
        _cancel.Click += (_, _) => RequestCancel(false);
        _copy.Click += (_, _) => { if (!string.IsNullOrEmpty(_log.Text)) Clipboard.SetText(_log.Text); };
        buttons.Controls.Add(_close);
        buttons.Controls.Add(_cancel);
        buttons.Controls.Add(_copy);
        root.Controls.Add(buttons);
    }

    void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_running) return;
        e.Cancel = true;
        if (_cts.IsCancellationRequested) return;
        if (MessageBox.Show(this,
            "Cancel AI runtime setup?\n\nCompleted packages and resumable download cache will be kept. You can run Repair AI Runtime again to continue.",
            "Cancel AI runtime setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            RequestCancel(true);
    }

    void RequestCancel(bool closeWhenStopped)
    {
        if (!_running || _cts.IsCancellationRequested) return;
        _closeAfterCancel |= closeWhenStopped;
        _cancel.Enabled = false;
        _status.Text = "Cancelling safely…";
        _detail.Text = "Stopping setup. Download cache and already installed packages are being preserved for the next Repair attempt.";
        AppendLog($"[{DateTime.Now:HH:mm:ss}] Cancellation requested; resumable runtime cache will be preserved.");
        _cts.Cancel();
    }

    async Task RunAsync()
    {
        _running = true;
        AppendLog($"[{DateTime.Now:HH:mm:ss}] AI runtime setup started");
        AppendLog($"Install folder: {_settings.InstallRoot}");
        AppendLog($"AI data/cache folder: {_settings.DataRoot}");
        try
        {
            var service = new RuntimeSetupService(_settings);
            var events = new Progress<RuntimeSetupEvent>(OnSetupEvent);
            string result = await service.RepairAsync(events, _cts.Token);
            Succeeded = true;
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = 100;
            _status.Text = "AI runtime setup completed successfully.";
            _detail.Text = $"Runtime dependencies and Xet support verified in {FormatElapsed(_elapsed.Elapsed)}.";
            AppendLog($"[{DateTime.Now:HH:mm:ss}] {result}");
        }
        catch (OperationCanceledException)
        {
            Cancelled = true;
            _progress.Style = ProgressBarStyle.Marquee;
            _status.Text = "AI runtime setup cancelled.";
            _detail.Text = "Download cache was preserved. Run Repair AI Runtime again to continue/reverify the environment.";
            AppendLog($"[{DateTime.Now:HH:mm:ss}] Setup cancelled; cached downloads preserved.");
        }
        catch (Exception ex)
        {
            FailureMessage = ex.Message;
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = 0;
            _status.Text = "AI runtime setup failed.";
            _detail.Text = "See the log below. Cached downloads are retained so a later Repair can retry or resume them.";
            AppendLog($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
        }
        finally
        {
            _running = false;
            _cancel.Enabled = false;
            _close.Enabled = true;
            _close.Focus();
            if (_closeAfterCancel && !IsDisposed) BeginInvoke(Close);
        }
    }

    void OnSetupEvent(RuntimeSetupEvent e)
    {
        string message = e.Message.Trim();
        if (string.IsNullOrWhiteSpace(message)) return;
        string prefix = IsActualError(message) ? "ERROR " : IsWarning(message) ? "WARN " : "";
        AppendLog($"[{e.Timestamp.LocalDateTime:HH:mm:ss}] {prefix}{message}");

        if (TryParsePercent(message, out int percent))
        {
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = Math.Clamp(percent, 0, 99);
        }
        if (message.Contains("Downloading PyTorch", StringComparison.OrdinalIgnoreCase)) _status.Text = "Downloading PyTorch runtime…";
        else if (message.Contains("torchvision", StringComparison.OrdinalIgnoreCase) && message.Contains("Downloading", StringComparison.OrdinalIgnoreCase)) _status.Text = "Downloading torchvision runtime…";
        else if (message.Contains("Collecting", StringComparison.OrdinalIgnoreCase) || message.Contains("Installing collected packages", StringComparison.OrdinalIgnoreCase)) _status.Text = "Installing Python packages…";
        else if (message.Contains("pip check", StringComparison.OrdinalIgnoreCase)) _status.Text = "Verifying Python environment…";
        else if (message.Contains("AI runtime is ready", StringComparison.OrdinalIgnoreCase)) _status.Text = "Final verification complete…";

        _detail.Text = $"Elapsed {FormatElapsed(_elapsed.Elapsed)} · runtime downloads use persistent cache and can resume after interruption.";
    }

    void AppendLog(string line)
    {
        if (IsDisposed) return;
        _log.AppendText(line + Environment.NewLine);
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    static bool IsActualError(string message) =>
        message.Contains("Traceback", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("Exception", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("failed", StringComparison.OrdinalIgnoreCase);

    static bool IsWarning(string message) =>
        message.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("deprecated", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("retry", StringComparison.OrdinalIgnoreCase);

    static bool TryParsePercent(string message, out int percent)
    {
        var match = Regex.Match(message, @"(?<!\d)(100|\d{1,2})(?:\.\d+)?\s*%");
        if (match.Success && double.TryParse(match.Groups[1].Value, out double parsed))
        {
            percent = (int)Math.Round(parsed);
            return true;
        }
        percent = 0;
        return false;
    }

    static string FormatElapsed(TimeSpan value) =>
        value.TotalHours >= 1 ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}" : $"{value.Minutes}:{value.Seconds:00}";
}