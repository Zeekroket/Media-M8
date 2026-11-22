using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace MediaM8;

internal static class UpdateResultDialog
{
    public static void ShowNewVersion(
        IWin32Window owner,
        Version installedVersion,
        Version latestVersion,
        string releaseUrl)
    {
        using var dialog = new Form
        {
            Text = "New Media M8 version available",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(470, 190),
            Font = new Font("Segoe UI", 9f)
        };

        var heading = new Label
        {
            Location = new Point(20, 20),
            Size = new Size(430, 30),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Text = $"Media M8 {latestVersion} is available."
        };
        dialog.Controls.Add(heading);

        var versions = new Label
        {
            Location = new Point(20, 60),
            Size = new Size(430, 42),
            Text =
                $"Installed version: {installedVersion}" +
                Environment.NewLine +
                $"Latest version: {latestVersion}"
        };
        dialog.Controls.Add(versions);

        var note = new Label
        {
            Location = new Point(20, 108),
            Size = new Size(430, 28),
            Text = "Media M8 will not install anything automatically."
        };
        dialog.Controls.Add(note);

        var releaseButton = new Button
        {
            Text = "Open release page",
            Location = new Point(270, 145),
            Size = new Size(110, 30)
        };
        releaseButton.Click += (_, _) =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = releaseUrl,
                UseShellExecute = true
            });
        };
        dialog.Controls.Add(releaseButton);

        var closeButton = new Button
        {
            Text = "Close",
            Location = new Point(390, 145),
            Size = new Size(60, 30),
            DialogResult = DialogResult.OK
        };
        dialog.Controls.Add(closeButton);

        dialog.AcceptButton = releaseButton;
        dialog.CancelButton = closeButton;

        dialog.ShowDialog(owner);
    }
}
