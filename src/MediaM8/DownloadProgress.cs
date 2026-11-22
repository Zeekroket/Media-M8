using System.Text.Json;

namespace MediaM8;

internal enum DownloadPhase
{
    Preparing,
    Downloading,
    Processing,
    Completed
}

internal sealed record DownloadProgressSnapshot(
    DownloadPhase Phase,
    double? Percentage,
    long? DownloadedBytes,
    long? TotalBytes,
    double? SpeedBytesPerSecond,
    int? EtaSeconds,
    string Status);

internal static class DownloadProgressParser
{
    public const string Marker = "MEDIA_M8_PROGRESS|";

    public static bool TryParse(
        string line,
        out DownloadProgressSnapshot? snapshot)
    {
        snapshot = null;

        if (!line.StartsWith(Marker, StringComparison.Ordinal))
        {
            return false;
        }

        var payload = line[Marker.Length..];
        var separatorIndex = payload.IndexOf('|');
        if (separatorIndex <= 0 || separatorIndex == payload.Length - 1)
        {
            return false;
        }

        var kind = payload[..separatorIndex];
        var json = payload[(separatorIndex + 1)..];

        try
        {
            using var document = JsonDocument.Parse(json);
            var progress = document.RootElement;

            var status = GetString(progress, "status") ?? string.Empty;
            var downloadedBytes = GetInt64(progress, "downloaded_bytes");
            var totalBytes =
                GetInt64(progress, "total_bytes") ??
                GetInt64(progress, "total_bytes_estimate");
            var speed = GetDouble(progress, "speed");
            var etaValue = GetDouble(progress, "eta");
            var etaSeconds = etaValue is null
                ? null
                : (int?)Math.Max(0, (int)Math.Round(etaValue.Value));

            double? percentage = null;
            if (downloadedBytes is >= 0 && totalBytes is > 0)
            {
                percentage = Math.Clamp(
                    downloadedBytes.Value * 100d / totalBytes.Value,
                    0d,
                    100d);
            }

            var phase = kind.Equals(
                "postprocess",
                StringComparison.OrdinalIgnoreCase)
                ? DownloadPhase.Processing
                : DownloadPhase.Downloading;

            snapshot = new DownloadProgressSnapshot(
                phase,
                percentage,
                downloadedBytes,
                totalBytes,
                speed,
                etaSeconds,
                status);

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
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

    private static long? GetInt64(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number)
        {
            if (property.TryGetInt64(out var integer))
            {
                return integer;
            }

            if (property.TryGetDouble(out var number))
            {
                return (long)number;
            }
        }

        return long.TryParse(property.ToString(), out var parsed)
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
            property.TryGetDouble(out var number))
        {
            return number;
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
