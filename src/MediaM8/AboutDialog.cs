using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace MediaM8;

internal static class AboutDialog
{
    private const string ProjectUrl =
        "https://github.com/Zeekroket/Media-M8";
    private const string HelpUrl =
        "https://github.com/Zeekroket/Media-M8/blob/main/HELP.md";
    private const string ReleasesUrl =
        "https://github.com/Zeekroket/Media-M8/releases";
    private const string IssuesUrl =
        "https://github.com/Zeekroket/Media-M8/issues";

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
            ClientSize = new Size(620, 510),
            Font = new Font("Segoe UI", 9f)
        };

        var logo = new PictureBox
        {
            Location = new Point(24, 24),
            Size = new Size(72, 72),
            SizeMode = PictureBoxSizeMode.Zoom
        };

        Bitmap? logoImage = null;

        try
        {
            using var icon = Icon.ExtractAssociatedIcon(
                Application.ExecutablePath);

            if (icon is not null)
            {
                logoImage = icon.ToBitmap();
                logo.Image = logoImage;
            }
        }
        catch
        {
            // The About dialog still works if the executable icon cannot be read.
        }

        dialog.Controls.Add(logo);

        var title = new Label
        {
            Location = new Point(118, 24),
            Size = new Size(475, 34),
            Font = new Font(
                "Segoe UI",
                16f,
                FontStyle.Bold),
            Text = $"{AppInfo.Name} {AppInfo.Version}"
        };
        dialog.Controls.Add(title);

        var pronunciation = new Label
        {
            Location = new Point(120, 60),
            Size = new Size(470, 22),
            Text = "Pronounced “Media Mate”"
        };
        dialog.Controls.Add(pronunciation);

        var description = new Label
        {
            Location = new Point(120, 86),
            Size = new Size(470, 42),
            Text =
                "A simple Windows app for downloading media from supported websites " +
                "without requiring command-line knowledge."
        };
        dialog.Controls.Add(description);

        var projectHeading = NewSectionHeading(
            "Project",
            24,
            145);
        dialog.Controls.Add(projectHeading);

        dialog.Controls.Add(
            CreateLink(
                dialog,
                "GitHub repository",
                ProjectUrl,
                24,
                174));
        dialog.Controls.Add(
            CreateLink(
                dialog,
                "Help & documentation",
                HelpUrl,
                24,
                199));
        dialog.Controls.Add(
            CreateLink(
                dialog,
                "Release notes",
                ReleasesUrl,
                24,
                224));
        dialog.Controls.Add(
            CreateLink(
                dialog,
                "Issue tracker",
                IssuesUrl,
                24,
                249));

        var licenceHeading = NewSectionHeading(
            "Licence",
            320,
            145);
        dialog.Controls.Add(licenceHeading);

        var licence = new Label
        {
            Location = new Point(320, 174),
            Size = new Size(275, 72),
            Text =
                "MIT License" +
                Environment.NewLine +
                "Copyright © 2026 Patrick M van Ree" +
                Environment.NewLine +
                "Open source software."
        };
        dialog.Controls.Add(licence);

        var componentsHeading = NewSectionHeading(
            "Third-party components",
            24,
            300);
        dialog.Controls.Add(componentsHeading);

        var acknowledgement = new Label
        {
            Location = new Point(24, 330),
            Size = new Size(570, 86),
            Text =
                "Media M8 uses open-source components including yt-dlp, FFmpeg / ffprobe " +
                "and Deno. These projects are independent from Media M8 and are governed " +
                "by their own licences and release terms."
        };
        dialog.Controls.Add(acknowledgement);

        var sourceNote = new Label
        {
            Location = new Point(24, 420),
            Size = new Size(430, 46),
            Text =
                "Source code, documentation and project history are available on GitHub."
        };
        dialog.Controls.Add(sourceNote);

        var closeButton = new Button
        {
            Text = "Close",
            Location = new Point(515, 455),
            Size = new Size(80, 32),
            DialogResult = DialogResult.OK
        };
        dialog.Controls.Add(closeButton);

        dialog.AcceptButton = closeButton;
        dialog.CancelButton = closeButton;

        ThemeManager.Apply(dialog);

        dialog.ShowDialog(owner);

        logo.Image = null;
        logoImage?.Dispose();
    }

    private static Label NewSectionHeading(
        string text,
        int x,
        int y)
    {
        return new Label
        {
            Location = new Point(x, y),
            Size = new Size(275, 24),
            Font = new Font(
                "Segoe UI",
                10f,
                FontStyle.Bold),
            Text = text
        };
    }

    private static LinkLabel CreateLink(
        IWin32Window owner,
        string text,
        string url,
        int x,
        int y)
    {
        var link = new LinkLabel
        {
            Location = new Point(x, y),
            Size = new Size(275, 22),
            Text = text,
            TabStop = true
        };

        link.LinkClicked += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show(
                    owner,
                    "Media M8 could not open this link.",
                    AppInfo.Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };

        return link;
    }
}
