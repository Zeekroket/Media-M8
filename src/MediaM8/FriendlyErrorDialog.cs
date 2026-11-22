using System.Drawing;
using System.Windows.Forms;

namespace MediaM8;

internal static class FriendlyErrorDialog
{
    public static void Show(
        IWin32Window owner,
        FriendlyDownloadFailure failure,
        string technicalDetails,
        string sessionLogPath)
    {
        using var dialog = new Form
        {
            Text = failure.Title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(520, 220),
            Font = new Font("Segoe UI", 9f)
        };

        var message = new Label
        {
            Location = new Point(20, 20),
            Size = new Size(480, 60),
            Text = failure.Message
        };
        dialog.Controls.Add(message);

        var nextAction = new Label
        {
            Location = new Point(20, 88),
            Size = new Size(480, 48),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Text = failure.NextAction
        };
        dialog.Controls.Add(nextAction);

        var detailsBox = new TextBox
        {
            Location = new Point(20, 150),
            Size = new Size(480, 210),
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            ReadOnly = true,
            Font = new Font("Consolas", 9f),
            Visible = false,
            Text =
                technicalDetails.Trim() +
                Environment.NewLine +
                Environment.NewLine +
                "Session log: " +
                sessionLogPath
        };
        dialog.Controls.Add(detailsBox);

        var detailsButton = new Button
        {
            Text = "Details...",
            Location = new Point(320, 170),
            Size = new Size(85, 30)
        };
        dialog.Controls.Add(detailsButton);

        var closeButton = new Button
        {
            Text = "Close",
            Location = new Point(415, 170),
            Size = new Size(85, 30),
            DialogResult = DialogResult.OK
        };
        dialog.Controls.Add(closeButton);

        detailsButton.Click += (_, _) =>
        {
            var showDetails = !detailsBox.Visible;
            detailsBox.Visible = showDetails;
            detailsButton.Text = showDetails ? "Hide details" : "Details...";

            if (showDetails)
            {
                dialog.ClientSize = new Size(520, 420);
                detailsButton.Location = new Point(305, 370);
                closeButton.Location = new Point(415, 370);
            }
            else
            {
                dialog.ClientSize = new Size(520, 220);
                detailsButton.Location = new Point(320, 170);
                closeButton.Location = new Point(415, 170);
            }
        };

        dialog.AcceptButton = closeButton;
        dialog.CancelButton = closeButton;

        ThemeManager.Apply(dialog);

        dialog.ShowDialog(owner);
    }
}
