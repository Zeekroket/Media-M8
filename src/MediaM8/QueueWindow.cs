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
    private readonly Button _retryButton = new();
    private readonly Button _moveUpButton = new();
    private readonly Button _moveDownButton = new();

    public QueueWindow(
        DownloadQueueModel queue,
        Func<Task> resumeQueueAsync)
    {
        _queue = queue;
        _resumeQueueAsync = resumeQueueAsync;

        Text = "Media M8 Queue";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(705, 381);
        MinimumSize = new Size(620, 340);
        Font = new Font("Segoe UI", 9f);

        BuildUi();
        WindowLayout.Prepare(this);
        Load += (_, _) =>
        {
            _items.Columns[0].Width = WindowLayout.Pixels(this, 115);
            _items.Columns[1].Width = WindowLayout.Pixels(this, 525);
        };

        _queue.Changed += Queue_Changed;
        ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        FormClosed += (_, _) =>
        {
            ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            _queue.Changed -= Queue_Changed;
        };

        ApplyCurrentTheme();
        RefreshQueue();
    }

    private void BuildUi()
    {
        _items.Location = new Point(20, 20);
        _items.Size = new Size(665, 243);
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
        _items.DrawColumnHeader += Items_DrawColumnHeader;
        _items.DrawSubItem += Items_DrawSubItem;
        _items.DrawItem += (_, e) =>
        {
            if (_items.View != View.Details)
            {
                e.DrawDefault = true;
            }
        };
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

        _retryButton.Text = "Retry";
        _retryButton.Location = new Point(20, 278);
        _retryButton.Size = new Size(120, 32);
        _retryButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _retryButton.Click += async (_, _) =>
        {
            if (GetSelectedQueueItem() is not { } selected ||
                _queue.Retry(selected.Id) is not { } retried)
            {
                return;
            }

            foreach (ListViewItem row in _items.Items)
            {
                if (row.Tag is int id && id == retried.Id)
                {
                    row.Selected = true;
                    row.Focused = true;
                    row.EnsureVisible();
                    break;
                }
            }

            if (!_queue.IsPaused)
            {
                await _resumeQueueAsync();
            }
        };
        Controls.Add(_retryButton);

        _moveUpButton.Text = "Move up";
        _moveUpButton.Location = new Point(150, 278);
        _moveUpButton.Size = new Size(115, 32);
        _moveUpButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _moveUpButton.Click += (_, _) => MoveSelectedPending(-1);
        Controls.Add(_moveUpButton);

        _moveDownButton.Text = "Move down";
        _moveDownButton.Location = new Point(275, 278);
        _moveDownButton.Size = new Size(105, 32);
        _moveDownButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _moveDownButton.Click += (_, _) => MoveSelectedPending(1);
        Controls.Add(_moveDownButton);

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

        _items.AccessibleName = "Download queue";
        _items.TabIndex = 0;
        _retryButton.TabIndex = 1;
        _moveUpButton.TabIndex = 2;
        _moveDownButton.TabIndex = 3;
        _pauseResumeButton.TabIndex = 4;
        _removePendingButton.TabIndex = 5;
        _clearFinishedButton.TabIndex = 6;
        closeButton.TabIndex = 7;

        AcceptButton = closeButton;
        CancelButton = closeButton;
    }

    private void ThemeManager_ThemeChanged(
        object? sender,
        EventArgs e)
    {
        ApplyCurrentTheme();
        RefreshQueue();
    }

    private void ApplyCurrentTheme()
    {
        ThemeManager.Apply(this);

        _items.OwnerDraw =
            ThemeManager.IsDarkMode;

        _items.Invalidate();
        Invalidate(invalidateChildren: true);
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
                    queueItem.State is DownloadQueueState.Skipped or DownloadQueueState.CompletedWithSkips
                        ? $"{queueItem.StatusDetail} · {queueItem.Request.DisplayName}"
                        : queueItem.Request.DisplayName);

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

        var selected = GetSelectedQueueItem();

        _removePendingButton.Enabled =
            selected?.State == DownloadQueueState.Pending;

        _retryButton.Enabled =
            selected?.State is DownloadQueueState.Failed or DownloadQueueState.Stopped;

        _moveUpButton.Enabled =
            selected is not null && _queue.CanMovePending(selected.Id, -1);

        _moveDownButton.Enabled =
            selected is not null && _queue.CanMovePending(selected.Id, 1);

        _clearFinishedButton.Enabled =
            _queue.Items.Any(
                item =>
                    item.State == DownloadQueueState.Completed ||
                    item.State == DownloadQueueState.CompletedWithSkips ||
                    item.State == DownloadQueueState.Skipped ||
                    item.State == DownloadQueueState.Failed ||
                    item.State == DownloadQueueState.Stopped);
    }

    private void MoveSelectedPending(int direction)
    {
        if (GetSelectedQueueItem() is { } selected)
        {
            _queue.MovePending(selected.Id, direction);
        }
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

    private void Items_DrawColumnHeader(
        object? sender,
        DrawListViewColumnHeaderEventArgs e)
    {
        using var background = new SolidBrush(
            ThemeManager.InputBackColor);
        using var border = new Pen(
            ThemeManager.BorderColor);

        e.Graphics.FillRectangle(
            background,
            e.Bounds);
        e.Graphics.DrawRectangle(
            border,
            e.Bounds);

        TextRenderer.DrawText(
            e.Graphics,
            e.Header?.Text ?? string.Empty,
            Font,
            e.Bounds,
            ThemeManager.TextColor,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);
    }

    private void Items_DrawSubItem(
        object? sender,
        DrawListViewSubItemEventArgs e)
    {
        var selected = e.Item.Selected;

        using var background = new SolidBrush(
            selected
                ? ThemeManager.SelectionBackColor
                : ThemeManager.InputBackColor);

        e.Graphics.FillRectangle(
            background,
            e.Bounds);

        if (e.Item.Focused && _items.Focused)
        {
            ControlPaint.DrawFocusRectangle(
                e.Graphics, e.Bounds,
                ThemeManager.SelectionTextColor,
                ThemeManager.SelectionBackColor);
        }

        TextRenderer.DrawText(
            e.Graphics,
            e.SubItem.Text,
            Font,
            e.Bounds,
            selected
                ? ThemeManager.SelectionTextColor
                : ThemeManager.TextColor,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);
    }

    private static string FormatState(
        DownloadQueueState state)
    {
        return state switch
        {
            DownloadQueueState.Downloading => "Downloading",
            DownloadQueueState.Completed => "Completed",
            DownloadQueueState.CompletedWithSkips => "With skips",
            DownloadQueueState.Skipped => "Skipped",
            DownloadQueueState.Failed => "Failed",
            DownloadQueueState.Stopped => "Stopped",
            _ => "Pending"
        };
    }
}
