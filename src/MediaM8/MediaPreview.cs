using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MediaM8;

internal sealed record MediaPreviewInfo(
    string Title,
    string? Uploader,
    TimeSpan? Duration,
    int? PlaylistCount,
    bool IsPlaylist);

internal static class MediaPreviewLoader
{
    public static async Task<MediaPreviewInfo> LoadAsync(
        string ytDlpPath,
        string sourceUrl,
        bool isYouTube,
        string denoPath,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "--dump-single-json",
            "--flat-playlist",
            "--yes-playlist",
            "--no-warnings"
        };

        if (isYouTube)
        {
            arguments.Add("--extractor-args");
            arguments.Add("youtube:player_client=default,-web_embedded");
        }

        arguments.Add("--js-runtimes");
        arguments.Add("deno:" + denoPath);
        arguments.Add(sourceUrl);

        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Could not start metadata preview.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
        {
            throw new DownloadFailureException(
                "Preview metadata could not be loaded.",
                $"yt-dlp preview exited with code {process.ExitCode}." +
                Environment.NewLine +
                stderr.Trim());
        }

        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;

        var title =
            GetString(root, "title") ??
            GetString(root, "fulltitle") ??
            "Untitled media";

        var uploader =
            GetString(root, "uploader") ??
            GetString(root, "channel") ??
            GetString(root, "creator");

        var isPlaylist =
            string.Equals(
                GetString(root, "_type"),
                "playlist",
                StringComparison.OrdinalIgnoreCase) ||
            root.TryGetProperty("entries", out _);

        int? playlistCount = null;

        if (isPlaylist)
        {
            playlistCount =
                GetInt32(root, "playlist_count") ??
                GetInt32(root, "n_entries");

            if (playlistCount is null &&
                root.TryGetProperty("entries", out var entries) &&
                entries.ValueKind == JsonValueKind.Array)
            {
                playlistCount = entries.GetArrayLength();
            }
        }

        TimeSpan? duration = null;

        if (!isPlaylist)
        {
            var durationSeconds = GetDouble(root, "duration");
            if (durationSeconds is >= 0)
            {
                duration = TimeSpan.FromSeconds(durationSeconds.Value);
            }
        }

        return new MediaPreviewInfo(
            title,
            uploader,
            duration,
            playlistCount,
            isPlaylist);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ToString();
    }

    private static int? GetInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number &&
            property.TryGetInt32(out var value))
        {
            return value;
        }

        return int.TryParse(property.ToString(), out var parsed)
            ? parsed
            : null;
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number &&
            property.TryGetDouble(out var value))
        {
            return value;
        }

        return double.TryParse(
            property.ToString(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
    }
}
