using System.Text.Json;

namespace MediaM8;

internal sealed record UpdateAsset(string Name, string Url, string Sha256, Version Version);

internal static class UpdateAssetSelector
{
    private const string ReleaseDownloadPrefix =
        "https://github.com/Zeekroket/Media-M8/releases/download/";

    public static UpdateAsset Select(
        JsonElement root, Version expectedVersion, Version installedVersion, bool isPortable)
    {
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (root.GetProperty("draft").GetBoolean() ||
            root.GetProperty("prerelease").GetBoolean() ||
            !Version.TryParse(tag.TrimStart('v', 'V'), out var version) ||
            version != expectedVersion ||
            version <= installedVersion)
        {
            throw new InvalidOperationException("The available release has changed. Check for updates again.");
        }

        var suffix = isPortable ? "portable.zip" : "setup.exe";
        var name = $"MediaM8-{version}-win-x64-{suffix}";
        JsonElement asset = default;
        foreach (var candidate in root.GetProperty("assets").EnumerateArray())
        {
            if (candidate.GetProperty("name").GetString() == name)
            {
                asset = candidate;
                break;
            }
        }

        if (asset.ValueKind == JsonValueKind.Undefined ||
            !asset.TryGetProperty("digest", out var digestElement) ||
            digestElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("A verified update package is not available for this version.");
        }

        var digest = digestElement.GetString() ?? "";
        if (!digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ||
            !IsSha256(digest[7..]))
        {
            throw new InvalidOperationException("The release does not publish a valid SHA-256 checksum.");
        }

        var url = asset.GetProperty("browser_download_url").GetString() ?? "";
        if (!url.StartsWith(ReleaseDownloadPrefix, StringComparison.Ordinal) ||
            !url.EndsWith("/" + name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The update download address is unexpected.");
        }

        return new UpdateAsset(name, url, digest[7..], version);
    }

    internal static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);
}
