namespace MediaM8;

internal sealed record DownloadProcessResult(
    int ExitCode,
    string TechnicalDetails,
    bool ArchiveOnly,
    int ArchiveSkipCount);

internal sealed class DownloadFailureException : Exception
{
    public DownloadFailureException(
        string message,
        string technicalDetails,
        Exception? innerException = null)
        : base(message, innerException)
    {
        TechnicalDetails = technicalDetails;
    }

    public string TechnicalDetails { get; }
}

internal sealed record FriendlyDownloadFailure(
    string Title,
    string Message,
    string NextAction,
    string Status)
{
    public static FriendlyDownloadFailure From(Exception exception)
    {
        if (exception is DestinationFolderException destination)
            return ForDestination(destination.Problem);

        var technicalDetails = exception is DownloadFailureException downloadFailure
            ? downloadFailure.TechnicalDetails
            : exception.ToString();

        var haystack =
            (exception.Message + Environment.NewLine + technicalDetails)
            .ToLowerInvariant();

        if (exception is DownloadFailureException &&
            TryGetStorageProblem(technicalDetails) is { } storageProblem)
            return ForDestination(storageProblem);

        if (ContainsAny(
                haystack,
                "private video",
                "video is private",
                "this video is private",
                "private playlist"))
        {
            return new FriendlyDownloadFailure(
                "This media is private",
                "Media M8 cannot download this because it is not publicly accessible.",
                "Use a link that you can open without signing in, then try again.",
                "This media is private.");
        }

        if (ContainsAny(
                haystack,
                "sign in",
                "login required",
                "log in",
                "age-restricted",
                "age restricted",
                "confirm your age",
                "cookies are required"))
        {
            return new FriendlyDownloadFailure(
                "Sign-in is required",
                "This media can only be accessed after signing in or confirming access.",
                "Open the link in your browser to check that you can view it, then try again.",
                "Sign-in is required.");
        }

        if (ContainsAny(
                haystack,
                "video unavailable",
                "this video is unavailable",
                "media unavailable",
                "has been removed",
                "has been deleted",
                "not available in your country",
                "not available in your region"))
        {
            return new FriendlyDownloadFailure(
                "This media is not available",
                "The website says this media cannot currently be downloaded.",
                "Check that the link still works in your browser, then try again.",
                "This media is not available.");
        }

        if (ContainsAny(
                haystack,
                "unsupported url",
                "unsupported url:",
                "no suitable extractor",
                "url is not supported"))
        {
            return new FriendlyDownloadFailure(
                "This link is not supported",
                "Media M8 does not know how to download from this link.",
                "Check that you copied the direct page link for the video, audio or playlist.",
                "This link is not supported.");
        }

        if (ContainsAny(
                haystack,
                "timed out",
                "timeout",
                "connection reset",
                "connection refused",
                "temporary failure in name resolution",
                "name or service not known",
                "no such host",
                "network is unreachable"))
        {
            return new FriendlyDownloadFailure(
                "Connection problem",
                "Media M8 could not keep a connection to the website.",
                "Check your internet connection and try again.",
                "Connection problem.");
        }

        if (ContainsAny(
                haystack,
                "no space left",
                "disk full",
                "not enough space",
                "insufficient disk space"))
        {
            return ForDestination(DestinationProblem.DiskFull);
        }

        // Match the actual yt-dlp error line, not arbitrary titles or warnings.
        // This specific Python exception is a known extractor failure pattern.
        // Require VK evidence before attributing it to that extractor.
        if (exception is DownloadFailureException &&
            IsKnownVkExtractorFailure(technicalDetails))
        {
            return new FriendlyDownloadFailure(
                "Download tool problem",
                "The download tool could not process this VK link. The website or download tool may have changed.",
                "Try Tools → Update download tools, then try again later.",
                "Download tool problem.");
        }

        return new FriendlyDownloadFailure(
            "Download could not be completed",
            "Media M8 could not finish this download.",
            "Check the link and your internet connection, then try again.",
            "Download could not be completed.");
    }

    private static FriendlyDownloadFailure ForDestination(DestinationProblem problem) =>
        problem switch
        {
            DestinationProblem.DiskFull => new(
                "Not enough free space",
                "There is not enough free space to finish this download.",
                "Free some space or choose another Save to folder, then try again.",
                "Not enough free space."),
            DestinationProblem.AccessDenied => new(
                "Cannot write to download folder",
                "Media M8 or its download tool does not have permission to write the required files.",
                "Check folder permissions or choose another Save to folder, then try again.",
                "Cannot write to download folder."),
            _ => new(
                "Download folder unavailable",
                "Media M8 cannot access the selected download folder.",
                "Reconnect the drive, check the folder or choose another Save to folder.",
                "Download folder unavailable.")
        };

    private static DestinationProblem? TryGetStorageProblem(string details)
    {
        foreach (var raw in details.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (!line.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase) &&
                !line.StartsWith("PermissionError:", StringComparison.OrdinalIgnoreCase) &&
                !line.StartsWith("OSError:", StringComparison.OrdinalIgnoreCase))
                continue;

            if (ContainsAny(line, "no space left on device", "disk full", "not enough space on the disk",
                "[errno 28]", "[winerror 112]", "[winerror 39]"))
                return DestinationProblem.DiskFull;

            // Distinguish local filesystem permissions from HTTP 403/website access.
            var fileContext = ContainsAny(line, "unable to open for writing", "unable to create directory",
                "postprocessing:", "error opening output", "permissionerror:", "unable to write");
            if (ContainsAny(line, "[errno 13]", "[winerror 5]", "[winerror 19]", "read-only file system") ||
                (fileContext && ContainsAny(line, "permission denied", "access is denied", "access denied")))
                return DestinationProblem.AccessDenied;

            if (fileContext && ContainsAny(line, "no such file or directory", "[winerror 3]",
                "[winerror 15]", "[winerror 21]", "the device is not ready", "network path was not found"))
                return DestinationProblem.Unavailable;
        }

        return null;
    }

    private static bool IsKnownVkExtractorFailure(string details)
    {
        var lines = details.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim()).ToArray();

        var hasVkContext = lines.Any(line =>
            line.StartsWith("[vk]", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("[vk:video]", StringComparison.OrdinalIgnoreCase) ||
            (line.StartsWith("File ", StringComparison.Ordinal) &&
             line.Replace('\\', '/').Contains("/extractor/vk.py", StringComparison.OrdinalIgnoreCase)));

        var hasKnownError = lines.Any(line =>
            line.Equals("ERROR: 'NoneType' object has no attribute 'split'", StringComparison.Ordinal) ||
            line.Equals("AttributeError: 'NoneType' object has no attribute 'split'", StringComparison.Ordinal));

        return hasVkContext && hasKnownError;
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value =>
            text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}
