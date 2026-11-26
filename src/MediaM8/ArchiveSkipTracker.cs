namespace MediaM8;

// One tracker per yt-dlp invocation. Call Observe under the process-output lock.
internal sealed class ArchiveSkipTracker
{
    private const string DownloadPrefix = "[download] ";
    private const string ArchiveSuffix = " has already been recorded in the archive";

    public int ArchiveSkipCount { get; private set; }
    public bool HasMediaResult { get; private set; }

    public void Observe(string line)
    {
        var text = line.Trim();
        if (text.StartsWith(DownloadPrefix, StringComparison.Ordinal) &&
            text.EndsWith(ArchiveSuffix, StringComparison.Ordinal))
        {
            ArchiveSkipCount++;
            return;
        }

        // A process can emit both archive skips and actual media results.
        // Distinguish the ordinary existing-file message from an archive skip.
        if (text.StartsWith(DownloadPrefix, StringComparison.Ordinal) &&
            text.EndsWith(" has already been downloaded", StringComparison.Ordinal))
        {
            HasMediaResult = true;
        }
        else if (text.StartsWith(DownloadProgressParser.Marker, StringComparison.Ordinal) &&
                 DownloadProgressParser.TryParse(text, out var progress) &&
                 progress is not null)
        {
            HasMediaResult = true;
        }
    }

    public bool IsArchiveOnly(bool archiveEnabled, int exitCode) =>
        archiveEnabled && exitCode == 0 && ArchiveSkipCount > 0 && !HasMediaResult;
}

internal sealed class DownloadItemResults
{
    public int Completed { get; private set; }
    public int Skipped { get; private set; }

    public void Record(bool archiveOnly, int archiveSkipCount)
    {
        if (!archiveOnly) Completed++;
        Skipped += archiveSkipCount;
    }

    public DownloadQueueState State => Skipped == 0
        ? DownloadQueueState.Completed
        : Completed == 0 ? DownloadQueueState.Skipped : DownloadQueueState.CompletedWithSkips;

    public string Detail => State switch
    {
        DownloadQueueState.Skipped => Skipped == 1
            ? "Skipped — already downloaded"
            : $"{Skipped} skipped — already downloaded",
        DownloadQueueState.CompletedWithSkips => $"{Completed} completed, {Skipped} skipped",
        _ => "Completed"
    };
}

internal sealed record QueueItemOutcome(DownloadQueueState State, string Detail);
