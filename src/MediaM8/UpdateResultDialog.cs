using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace MediaM8;

internal static class UpdateResultDialog
{
    public static bool ShowNewVersion(
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
            ClientSize = new Size(490, 225),
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
            Text = AppPaths.IsPortable
                ? "Your portable tools, logs and settings will be kept."
                : "Your settings and downloaded media will be kept."
        };
        dialog.Controls.Add(note);

        var releaseButton = new Button
        {
            Text = "Open release page",
            Location = new Point(20, 180),
            Size = new Size(130, 30)
        };
        releaseButton.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = releaseUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show(
                    dialog,
                    "Media M8 could not open the release page.",
                    AppInfo.Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };
        dialog.Controls.Add(releaseButton);

        var closeButton = new Button
        {
            Text = "Close",
            Location = new Point(410, 180),
            Size = new Size(60, 30),
            DialogResult = DialogResult.OK
        };
        dialog.Controls.Add(closeButton);

        var downloadButton = new Button
        {
            Text = "Download update",
            Location = new Point(260, 180),
            Size = new Size(140, 30),
            DialogResult = DialogResult.Yes
        };
        dialog.Controls.Add(downloadButton);

        dialog.AcceptButton = downloadButton;
        dialog.CancelButton = closeButton;

        ThemeManager.Apply(dialog);

        return dialog.ShowDialog(owner) == DialogResult.Yes;
    }
}
