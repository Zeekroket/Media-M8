namespace MediaM8;

internal enum DownloadQueueState
{
    Pending,
    Downloading,
    Completed,
    Failed,
    Stopped
}

internal sealed record DownloadRequest(
    string Url,
    string DisplayName,
    string Folder,
    bool DownloadPlaylist,
    bool SkipPreviouslyDownloaded,
    bool OpenFolderWhenFinished,
    bool DownloadAsMp3,
    string AudioQuality,
    string VideoQuality);

internal sealed class DownloadQueueItem
{
    public DownloadQueueItem(
        int id,
        DownloadRequest request)
    {
        Id = id;
        Request = request;
    }

    public int Id { get; }

    public DownloadRequest Request { get; }

    public DownloadQueueState State { get; internal set; } =
        DownloadQueueState.Pending;

    public string? StatusDetail { get; internal set; }
}

internal sealed class DownloadQueueModel
{
    private readonly List<DownloadQueueItem> _items = new();
    private int _nextId = 1;

    public IReadOnlyList<DownloadQueueItem> Items => _items;

    public int PendingCount =>
        _items.Count(item => item.State == DownloadQueueState.Pending);

    public DownloadQueueItem Enqueue(DownloadRequest request)
    {
        var item = new DownloadQueueItem(
            _nextId++,
            request);

        _items.Add(item);
        return item;
    }

    public DownloadQueueItem? GetNextPending()
    {
        return _items.FirstOrDefault(
            item => item.State == DownloadQueueState.Pending);
    }
}
