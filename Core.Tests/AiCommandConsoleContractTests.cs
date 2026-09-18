using System.Runtime.CompilerServices;

internal static class AiCommandConsoleContractTests
{
    [ModuleInitializer]
    internal static void ValidateExactAiCommandHistory()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "Main.AiCommandConsole.cs");
        if (!File.Exists(path))
            throw new InvalidOperationException("TEST FAILED: AI command console source is missing");

        string source = File.ReadAllText(path);
        Assert(Count(source, "run.Pressed += async () => await ExecuteAiConsoleInputAsync(input.Text);") == 1 &&
               Count(source, "_ = ExecuteAiConsoleInputAsync(_aiCommandInput?.Text ?? \"\");") == 1,
            "Run or Ctrl+Enter no longer dispatches exactly once through the shared command entry point");

        int execute = source.IndexOf("async Task ExecuteAiConsoleInputAsync(string raw)", StringComparison.Ordinal);
        int dispatch = execute >= 0
            ? source.IndexOf("static bool TryAiConsolePrefix", execute, StringComparison.Ordinal)
            : -1;
        Assert(execute >= 0 && dispatch > execute, "AI command dispatcher source could not be isolated");
        string executeBody = source[execute..dispatch];
        int blankGuard = executeBody.IndexOf("if (string.IsNullOrWhiteSpace(raw))", StringComparison.Ordinal);
        int recordRaw = executeBody.IndexOf("RecordAiCommand(raw);", StringComparison.Ordinal);
        int trimForDispatch = executeBody.IndexOf("string text = raw.Trim();", StringComparison.Ordinal);
        Assert(blankGuard >= 0 && recordRaw > blankGuard && trimForDispatch > recordRaw,
            "raw nonblank command text is not recorded exactly before a separate trimmed dispatch value is created");

        int record = source.IndexOf("void RecordAiCommand(string text)", StringComparison.Ordinal);
        int refresh = record >= 0
            ? source.IndexOf("void RefreshAiCommandHistoryLabel()", record, StringComparison.Ordinal)
            : -1;
        Assert(record >= 0 && refresh > record, "AI command history recorder source could not be isolated");
        string recordBody = source[record..refresh];
        Assert(recordBody.Contains("string.IsNullOrWhiteSpace(text)", StringComparison.Ordinal) &&
               !recordBody.Contains(".Trim()", StringComparison.Ordinal),
            "history recording still normalizes leading/trailing spaces or line breaks");
        Assert(source.Contains("_aiCommandInput.Text = _aiCommandHistory[_aiCommandHistoryIndex];", StringComparison.Ordinal),
            "history navigation does not restore the exact stored command text");
    }

    static int Count(string source, string value)
    {
        int count = 0;
        int start = 0;
        while ((start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }
        return count;
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("TEST FAILED: " + message);
    }
}
