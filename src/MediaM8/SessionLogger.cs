using System.Text;

namespace MediaM8;

internal sealed class SessionLogger : IDisposable
{
    private readonly object _sync = new();
    private readonly StreamWriter _writer;

    public SessionLogger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);

        LogFilePath = Path.Combine(
            logDirectory,
            $"MediaM8-{DateTime.Now:yyyyMMdd-HHmmss}.log");

        _writer = new StreamWriter(
            new FileStream(LogFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };
    }

    public string LogFilePath { get; }

    public void Write(string message, string? category = null)
    {
        var prefix = string.IsNullOrWhiteSpace(category) ? string.Empty : $"{category}: ";
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {prefix}{message}";

        lock (_sync)
        {
            _writer.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _writer.Dispose();
        }
    }
}
