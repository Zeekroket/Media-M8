namespace MediaM8;

internal sealed record DownloadProcessResult(
    int ExitCode,
    string TechnicalDetails);

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
        var technicalDetails = exception is DownloadFailureException downloadFailure
            ? downloadFailure.TechnicalDetails
            : exception.ToString();

        var haystack =
            (exception.Message + Environment.NewLine + technicalDetails)
            .ToLowerInvariant();

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
            return new FriendlyDownloadFailure(
                "Not enough free space",
                "There is not enough free space to finish this download.",
                "Free some space or choose another Save to folder, then try again.",
                "Not enough free space.");
        }

        return new FriendlyDownloadFailure(
            "Download could not be completed",
            "Media M8 could not finish this download.",
            "Check the link and your internet connection, then try again.",
            "Download could not be completed.");
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value =>
            text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}
