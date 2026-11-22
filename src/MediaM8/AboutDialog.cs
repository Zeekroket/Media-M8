using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace MediaM8;

internal static class AboutDialog
{
    private const string ProjectUrl =
        "https://github.com/Zeekroket/Media-M8";

    public static void Show(IWin32Window owner)
    {
        using var dialog = new Form
        {
            Text = "About Media M8",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(470, 300),
            Font = new Font("Segoe UI", 9f)
        };

        var title = new Label
        {
            Location = new Point(20, 20),
            Size = new Size(430, 30),
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            Text = $"{AppInfo.Name} {AppInfo.Version}"
        };
        dialog.Controls.Add(title);

        var pronunciation = new Label
        {
            Location = new Point(20, 55),
            Size = new Size(430, 24),
            Text = "Pronounced “Media Mate”"
        };
        dialog.Controls.Add(pronunciation);

        var description = new Label
        {
            Location = new Point(20, 88),
            Size = new Size(430, 52),
            Text =
                "A simple Windows app for downloading media from supported websites."
        };
        dialog.Controls.Add(description);

        var acknowledgement = new Label
        {
            Location = new Point(20, 145),
            Size = new Size(430, 72),
            Text =
                "Media M8 uses open-source components including yt-dlp, FFmpeg and Deno. " +
                "Those projects remain independent and are governed by their own licences."
        };
        dialog.Controls.Add(acknowledgement);

        var projectLink = new LinkLabel
        {
            Location = new Point(20, 222),
            Size = new Size(430, 24),
            Text = ProjectUrl
        };
        projectLink.LinkClicked += (_, _) =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ProjectUrl,
                UseShellExecute = true
            });
        };
        dialog.Controls.Add(projectLink);

        var closeButton = new Button
        {
            Text = "Close",
            Location = new Point(370, 255),
            Size = new Size(80, 30),
            DialogResult = DialogResult.OK
        };
        dialog.Controls.Add(closeButton);

        dialog.AcceptButton = closeButton;
        dialog.CancelButton = closeButton;

        dialog.ShowDialog(owner);
    }
}
