using System.Drawing;
using System.Windows.Forms;

namespace MediaM8;

internal sealed class QueueWindow : Form
{
    private readonly DownloadQueueModel _queue;
    private readonly Func<Task> _resumeQueueAsync;

    private readonly ListView _items = new();
    private readonly Button _pauseResumeButton = new();
    private readonly Button _removePendingButton = new();
    private readonly Button _clearFinishedButton = new();

    public QueueWindow(
        DownloadQueueModel queue,
        Func<Task> resumeQueueAsync)
    {
        _queue = queue;
        _resumeQueueAsync = resumeQueueAsync;

        Text = "Media M8 Queue";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(720, 420);
        MinimumSize = new Size(620, 340);
        Font = new Font("Segoe UI", 9f);

        BuildUi();

        _queue.Changed += Queue_Changed;
        FormClosed += (_, _) =>
        {
            _queue.Changed -= Queue_Changed;
        };

        RefreshQueue();
    }

    private void BuildUi()
    {
        _items.Location = new Point(20, 20);
        _items.Size = new Size(665, 285);
        _items.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Bottom |
            AnchorStyles.Left |
            AnchorStyles.Right;
        _items.View = View.Details;
        _items.FullRowSelect = true;
        _items.HideSelection = false;
        _items.MultiSelect = false;
        _items.Columns.Add("Status", 115);
        _items.Columns.Add("Item", 525);
        _items.SelectedIndexChanged += (_, _) =>
            UpdateButtons();
        Controls.Add(_items);

        _pauseResumeButton.Location = new Point(20, 320);
        _pauseResumeButton.Size = new Size(120, 32);
        _pauseResumeButton.Anchor =
            AnchorStyles.Bottom |
            AnchorStyles.Left;
        _pauseResumeButton.Click += async (_, _) =>
        {
            if (_queue.IsPaused)
            {
                _queue.SetPaused(false);
                await _resumeQueueAsync();
            }
            else
            {
                _queue.SetPaused(true);
            }
        };
        Controls.Add(_pauseResumeButton);

        _removePendingButton.Text = "Remove pending";
        _removePendingButton.Location = new Point(150, 320);
        _removePendingButton.Size = new Size(115, 32);
        _removePendingButton.Anchor =
            AnchorStyles.Bottom |
            AnchorStyles.Left;
        _removePendingButton.Click += (_, _) =>
        {
            if (_items.SelectedItems.Count != 1)
            {
                return;
            }

            if (_items.SelectedItems[0].Tag is int id)
            {
                _queue.RemovePending(id);
            }
        };
        Controls.Add(_removePendingButton);

        _clearFinishedButton.Text = "Clear finished";
        _clearFinishedButton.Location = new Point(275, 320);
        _clearFinishedButton.Size = new Size(105, 32);
        _clearFinishedButton.Anchor =
            AnchorStyles.Bottom |
            AnchorStyles.Left;
        _clearFinishedButton.Click += (_, _) =>
        {
            _queue.ClearFinished();
        };
        Controls.Add(_clearFinishedButton);

        var closeButton = new Button
        {
            Text = "Close",
            Location = new Point(605, 320),
            Size = new Size(80, 32),
            Anchor =
                AnchorStyles.Bottom |
                AnchorStyles.Right
        };
        closeButton.Click += (_, _) => Close();
        Controls.Add(closeButton);

        AcceptButton = closeButton;
        CancelButton = closeButton;
    }

    private void Queue_Changed(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke((Action)RefreshQueue);
            return;
        }

        RefreshQueue();
    }

    private void RefreshQueue()
    {
        int? selectedId = null;

        if (_items.SelectedItems.Count == 1 &&
            _items.SelectedItems[0].Tag is int id)
        {
            selectedId = id;
        }

        _items.BeginUpdate();

        try
        {
            _items.Items.Clear();

            foreach (var queueItem in _queue.Items)
            {
                var row = new ListViewItem(
                    FormatState(queueItem.State))
                {
                    Tag = queueItem.Id
                };

                row.SubItems.Add(
                    queueItem.Request.DisplayName);

                _items.Items.Add(row);

                if (selectedId == queueItem.Id)
                {
                    row.Selected = true;
                }
            }
        }
        finally
        {
            _items.EndUpdate();
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        _pauseResumeButton.Text = _queue.IsPaused
            ? "Resume Queue"
            : "Pause Queue";

        _removePendingButton.Enabled =
            GetSelectedQueueItem()?.State ==
            DownloadQueueState.Pending;

        _clearFinishedButton.Enabled =
            _queue.Items.Any(
                item =>
                    item.State == DownloadQueueState.Completed ||
                    item.State == DownloadQueueState.Failed ||
                    item.State == DownloadQueueState.Stopped);
    }

    private DownloadQueueItem? GetSelectedQueueItem()
    {
        if (_items.SelectedItems.Count != 1 ||
            _items.SelectedItems[0].Tag is not int id)
        {
            return null;
        }

        return _queue.Items.FirstOrDefault(
            item => item.Id == id);
    }

    private static string FormatState(
        DownloadQueueState state)
    {
        return state switch
        {
            DownloadQueueState.Downloading => "Downloading",
            DownloadQueueState.Completed => "Completed",
            DownloadQueueState.Failed => "Failed",
            DownloadQueueState.Stopped => "Stopped",
            _ => "Pending"
        };
    }
}
