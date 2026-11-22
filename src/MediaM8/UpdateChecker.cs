using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MediaM8;

internal enum UpdateAvailability
{
    NoPublishedRelease,
    UpToDate,
    NewVersionAvailable
}

internal sealed record UpdateCheckResult(
    UpdateAvailability Availability,
    Version InstalledVersion,
    Version? LatestVersion,
    string? ReleaseUrl);

internal static class UpdateChecker
{
    private const string LatestReleaseApiUrl =
        "https://api.github.com/repos/Zeekroket/Media-M8/releases/latest";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static async Task<UpdateCheckResult> CheckAsync(
        CancellationToken cancellationToken)
    {
        var installedVersion = ParseVersion(AppInfo.Version);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            LatestReleaseApiUrl);

        using var response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new UpdateCheckResult(
                UpdateAvailability.NoPublishedRelease,
                installedVersion,
                null,
                null);
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(
            cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        var root = document.RootElement;
        var tagName = GetRequiredString(root, "tag_name");
        var releaseUrl = GetRequiredString(root, "html_url");
        var latestVersion = ParseVersion(tagName);

        return new UpdateCheckResult(
            latestVersion > installedVersion
                ? UpdateAvailability.NewVersionAvailable
                : UpdateAvailability.UpToDate,
            installedVersion,
            latestVersion,
            releaseUrl);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(
                "MediaM8",
                AppInfo.Version));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));

        return client;
    }

    private static Version ParseVersion(string value)
    {
        var normalized = value.Trim();

        if (normalized.StartsWith(
                "v",
                StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[1..];
        }

        var prereleaseSeparator = normalized.IndexOf('-');
        if (prereleaseSeparator >= 0)
        {
            normalized = normalized[..prereleaseSeparator];
        }

        if (!Version.TryParse(normalized, out var version))
        {
            throw new InvalidOperationException(
                $"Could not parse Media M8 version '{value}'.");
        }

        return version;
    }

    private static string GetRequiredString(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException(
                $"GitHub release data did not contain '{propertyName}'.");
        }

        return property.GetString()!;
    }
}
