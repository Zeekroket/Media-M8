using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MediaM8;

internal enum RuntimeToolState
{
    Ready,
    Missing,
    UpdateAvailable
}

internal sealed record RuntimeToolHealthItem(
    string Name,
    RuntimeToolState State,
    string? InstalledVersion,
    string? LatestVersion,
    string Detail);

internal sealed record RuntimeToolHealthReport(
    IReadOnlyList<RuntimeToolHealthItem> Items)
{
    public RuntimeToolState OverallState =>
        Items.Any(item => item.State == RuntimeToolState.Missing)
            ? RuntimeToolState.Missing
            : Items.Any(item => item.State == RuntimeToolState.UpdateAvailable)
                ? RuntimeToolState.UpdateAvailable
                : RuntimeToolState.Ready;
}

internal static class RuntimeToolHealthChecker
{
    private const string YtDlpLatestReleaseApiUrl =
        "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
    private const string DenoLatestReleaseApiUrl =
        "https://api.github.com/repos/denoland/deno/releases/latest";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static async Task<RuntimeToolHealthReport> CheckAsync(
        string toolsDirectory,
        CancellationToken cancellationToken)
    {
        var paths = RuntimeToolManager.GetToolPaths(toolsDirectory);

        var ytDlpTask = CheckVersionedToolAsync(
            "yt-dlp",
            paths.YtDlp,
            "--version",
            YtDlpLatestReleaseApiUrl,
            ParseYtDlpVersion,
            cancellationToken);

        var denoTask = CheckVersionedToolAsync(
            "Deno",
            paths.Deno,
            "--version",
            DenoLatestReleaseApiUrl,
            ParseDenoVersion,
            cancellationToken);

        var ffmpegTask = CheckRunnableToolAsync(
            "FFmpeg",
            paths.FFmpeg,
            "-version",
            cancellationToken);

        var ffprobeTask = CheckRunnableToolAsync(
            "ffprobe",
            paths.FFprobe,
            "-version",
            cancellationToken);

        await Task.WhenAll(
            ytDlpTask,
            denoTask,
            ffmpegTask,
            ffprobeTask);

        return new RuntimeToolHealthReport(
            new[]
            {
                await ytDlpTask,
                await ffmpegTask,
                await ffprobeTask,
                await denoTask
            });
    }

    private static async Task<RuntimeToolHealthItem> CheckVersionedToolAsync(
        string name,
        string? path,
        string arguments,
        string latestReleaseApiUrl,
        Func<string, Version?> parseInstalledVersion,
        CancellationToken cancellationToken)
    {
        if (path is null)
        {
            return Missing(name);
        }

        var output = await RunToolAsync(
            path,
            arguments,
            cancellationToken);

        if (!output.Success)
        {
            return new RuntimeToolHealthItem(
                name,
                RuntimeToolState.Missing,
                null,
                null,
                $"{name} is present but could not be started.");
        }

        var installedVersion = parseInstalledVersion(output.Output);
        if (installedVersion is null)
        {
            return new RuntimeToolHealthItem(
                name,
                RuntimeToolState.Ready,
                null,
                null,
                $"{name} is installed and starts successfully. Version could not be compared reliably.");
        }

        try
        {
            var latestTag = await GetLatestReleaseTagAsync(
                latestReleaseApiUrl,
                cancellationToken);
            var latestVersion = ParseReleaseVersion(latestTag);

            if (latestVersion is not null &&
                latestVersion > installedVersion)
            {
                return new RuntimeToolHealthItem(
                    name,
                    RuntimeToolState.UpdateAvailable,
                    installedVersion.ToString(),
                    latestVersion.ToString(),
                    $"{name} is working, but a newer official release is available.");
            }

            return new RuntimeToolHealthItem(
                name,
                RuntimeToolState.Ready,
                installedVersion.ToString(),
                latestVersion?.ToString(),
                $"{name} is installed and working.");
        }
        catch
        {
            return new RuntimeToolHealthItem(
                name,
                RuntimeToolState.Ready,
                installedVersion.ToString(),
                null,
                $"{name} is installed and working. The latest version could not be checked.");
        }
    }

    private static async Task<RuntimeToolHealthItem> CheckRunnableToolAsync(
        string name,
        string? path,
        string arguments,
        CancellationToken cancellationToken)
    {
        if (path is null)
        {
            return Missing(name);
        }

        var output = await RunToolAsync(
            path,
            arguments,
            cancellationToken);

        if (!output.Success)
        {
            return new RuntimeToolHealthItem(
                name,
                RuntimeToolState.Missing,
                null,
                null,
                $"{name} is present but could not be started.");
        }

        var firstLine = output.Output
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .Trim();

        return new RuntimeToolHealthItem(
            name,
            RuntimeToolState.Ready,
            firstLine,
            null,
            $"{name} is installed and working. Automatic version comparison is not available for this tool.");
    }

    private static RuntimeToolHealthItem Missing(string name)
    {
        return new RuntimeToolHealthItem(
            name,
            RuntimeToolState.Missing,
            null,
            null,
            $"{name} is missing.");
    }

    private static async Task<(bool Success, string Output)> RunToolAsync(
        string path,
        string arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process
        {
            StartInfo = startInfo
        };

        try
        {
            if (!process.Start())
            {
                return (false, string.Empty);
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(
                cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(
                cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var output = string.IsNullOrWhiteSpace(stdout)
                ? stderr
                : stdout + Environment.NewLine + stderr;

            return (process.ExitCode == 0, output.Trim());
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    private static async Task<string> GetLatestReleaseTagAsync(
        string url,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(
            url,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);
        using var document = await System.Text.Json.JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty(
                "tag_name",
                out var tagProperty) ||
            tagProperty.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            throw new InvalidOperationException(
                "Release data did not contain a tag name.");
        }

        return tagProperty.GetString()!;
    }

    private static Version? ParseYtDlpVersion(string output)
    {
        var firstLine = output
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return firstLine is null
            ? null
            : ParseReleaseVersion(firstLine.Trim());
    }

    private static Version? ParseDenoVersion(string output)
    {
        var match = Regex.Match(
            output,
            @"\bdeno\s+(?<version>\d+(?:\.\d+){1,3})\b",
            RegexOptions.IgnoreCase);

        return match.Success &&
               Version.TryParse(
                   match.Groups["version"].Value,
                   out var version)
            ? version
            : null;
    }

    private static Version? ParseReleaseVersion(string value)
    {
        var normalized = value.Trim();

        if (normalized.StartsWith(
                "v",
                StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[1..];
        }

        var prerelease = normalized.IndexOf('-');
        if (prerelease >= 0)
        {
            normalized = normalized[..prerelease];
        }

        return Version.TryParse(normalized, out var version)
            ? version
            : null;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"MediaM8/{AppInfo.Version}");
        client.DefaultRequestHeaders.Accept.ParseAdd(
            "application/vnd.github+json");

        return client;
    }
}
