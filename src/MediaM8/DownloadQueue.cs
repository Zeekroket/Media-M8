namespace MediaM8;

internal enum DownloadQueueState
{
    Pending,
    Downloading,
    Completed,
    CompletedWithSkips,
    Skipped,
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

    public event EventHandler? Changed;

    public IReadOnlyList<DownloadQueueItem> Items => _items;

    public bool IsPaused { get; private set; }

    public int PendingCount =>
        _items.Count(item => item.State == DownloadQueueState.Pending);

    public DownloadQueueItem Enqueue(DownloadRequest request)
    {
        var item = new DownloadQueueItem(
            _nextId++,
            request);

        _items.Add(item);
        NotifyChanged();
        return item;
    }

    public DownloadQueueItem? GetNextPending()
    {
        return _items.FirstOrDefault(
            item => item.State == DownloadQueueState.Pending);
    }

    public void SetState(
        DownloadQueueItem item,
        DownloadQueueState state,
        string? statusDetail = null)
    {
        item.State = state;
        item.StatusDetail = statusDetail;
        NotifyChanged();
    }

    public bool RemovePending(int id)
    {
        var item = _items.FirstOrDefault(
            candidate =>
                candidate.Id == id &&
                candidate.State == DownloadQueueState.Pending);

        if (item is null)
        {
            return false;
        }

        _items.Remove(item);
        NotifyChanged();
        return true;
    }

    public DownloadQueueItem? Retry(int id)
    {
        var item = _items.FirstOrDefault(
            candidate => candidate.Id == id &&
                (candidate.State == DownloadQueueState.Failed ||
                 candidate.State == DownloadQueueState.Stopped));

        return item is null ? null : Enqueue(item.Request);
    }

    public bool CanMovePending(int id, int direction)
    {
        return FindPendingMove(id, direction) is not null;
    }

    public bool MovePending(int id, int direction)
    {
        if (FindPendingMove(id, direction) is not { } move)
        {
            return false;
        }

        (_items[move.From], _items[move.To]) =
            (_items[move.To], _items[move.From]);
        NotifyChanged();
        return true;
    }

    private (int From, int To)? FindPendingMove(int id, int direction)
    {
        if (direction != -1 && direction != 1)
        {
            return null;
        }

        var from = _items.FindIndex(
            item => item.Id == id &&
                item.State == DownloadQueueState.Pending);

        if (from < 0)
        {
            return null;
        }

        for (var to = from + direction;
             to >= 0 && to < _items.Count;
             to += direction)
        {
            if (_items[to].State == DownloadQueueState.Pending)
            {
                return (from, to);
            }
        }

        return null;
    }

    public int ClearFinished()
    {
        var removed = _items.RemoveAll(
            item =>
                item.State == DownloadQueueState.Completed ||
                item.State == DownloadQueueState.CompletedWithSkips ||
                item.State == DownloadQueueState.Skipped ||
                item.State == DownloadQueueState.Failed ||
                item.State == DownloadQueueState.Stopped);

        if (removed > 0)
        {
            NotifyChanged();
        }

        return removed;
    }

    public void SetPaused(bool paused)
    {
        if (IsPaused == paused)
        {
            return;
        }

        IsPaused = paused;
        NotifyChanged();
    }

    public void NotifyChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
