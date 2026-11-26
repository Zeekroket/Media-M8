using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MediaM8;

internal sealed record DownloadPlanItem(
    int Index,
    int Total,
    string Url,
    string? Title);

internal sealed record DownloadPlan(
    IReadOnlyList<DownloadPlanItem> Items,
    bool IsPlaylist);

internal static class DownloadPlanner
{
    public static async Task<DownloadPlan> PrepareAsync(
        string ytDlpPath,
        string sourceUrl,
        bool downloadPlaylist,
        bool isYouTube,
        string denoPath,
        SessionLogger logger,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!downloadPlaylist)
        {
            return new DownloadPlan(
                new[] { new DownloadPlanItem(1, 1, sourceUrl, null) },
                IsPlaylist: false);
        }

        logger.Write("Preparing playlist before download.", "PLAN");

        var arguments = new List<string>
        {
            "--flat-playlist",
            "--dump-json",
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

        var output = await PlaylistProcessRunner.RunAsync(
            startInfo, cancellationToken,
            ex => logger.Write($"Could not stop playlist preparation: {ex.Message}", "ERROR"))
            .ConfigureAwait(false);
        var stdout = output.Stdout;
        var stderr = output.Stderr;

        if (output.ExitCode != 0)
        {
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                logger.Write(stderr.Trim(), "PLAN");
            }

            throw new DownloadFailureException(
                "The playlist could not be prepared.",
                $"yt-dlp playlist preparation exited with code {output.ExitCode}." +
                Environment.NewLine +
                stderr.Trim());
        }

        var rawItems = new List<(string Url, string? Title)>();

        foreach (var line in stdout.Split(
                     new[] { "\r\n", "\n" },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            var itemUrl = GetItemUrl(root);
            if (string.IsNullOrWhiteSpace(itemUrl))
            {
                logger.Write(
                    "Skipped a prepared item because no downloadable URL was available.",
                    "PLAN");
                continue;
            }

            rawItems.Add((itemUrl, GetString(root, "title")));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (rawItems.Count == 0)
        {
            throw new DownloadFailureException(
                "No downloadable items were found.",
                "Playlist preparation completed but returned no downloadable items.");
        }

        var isPlaylist = rawItems.Count > 1;
        var total = rawItems.Count;
        var items = rawItems
            .Select((item, index) => new DownloadPlanItem(
                index + 1,
                total,
                item.Url,
                item.Title))
            .ToArray();

        logger.Write(
            isPlaylist
                ? $"Playlist prepared with {total} items."
                : "Prepared 1 download item.",
            "PLAN");

        return new DownloadPlan(items, isPlaylist);
    }

    private static string? GetItemUrl(JsonElement root)
    {
        foreach (var propertyName in new[] { "webpage_url", "original_url", "url" })
        {
            var value = GetString(root, propertyName);
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp ||
                 uri.Scheme == Uri.UriSchemeHttps))
            {
                return value;
            }
        }

        var id = GetString(root, "id");
        var extractorKey =
            GetString(root, "extractor_key") ??
            GetString(root, "ie_key");

        if (!string.IsNullOrWhiteSpace(id) &&
            extractorKey?.Contains(
                "youtube",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            return "https://www.youtube.com/watch?v=" +
                   Uri.EscapeDataString(id);
        }

        return null;
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
}
