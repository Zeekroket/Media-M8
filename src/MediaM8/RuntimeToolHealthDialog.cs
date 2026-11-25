using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MediaM8;

internal static class RuntimeToolHealthDialog
{
    public static void Show(
        IWin32Window owner,
        RuntimeToolHealthReport report)
    {
        var (title, heading, message) = report.OverallState switch
        {
            RuntimeToolState.Missing => (
                "Download tools need attention",
                "Download tools need repair.",
                "One or more required tools are missing or could not be started. Use Tools → Update download tools to repair them."),

            RuntimeToolState.UpdateAvailable => (
                "Download tool update available",
                "A download tool update is available.",
                "Media M8 can refresh its download tools from their official sources."),

            _ => (
                "Download tools are ready",
                "Download tools are ready.",
                "Media M8 has the required local tools and they can be started successfully.")
        };

        using var dialog = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(560, 210),
            Font = new Font("Segoe UI", 9f)
        };

        var headingLabel = new Label
        {
            Location = new Point(20, 20),
            Size = new Size(520, 30),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Text = heading
        };
        dialog.Controls.Add(headingLabel);

        var messageLabel = new Label
        {
            Location = new Point(20, 58),
            Size = new Size(520, 62),
            Text = message
        };
        dialog.Controls.Add(messageLabel);

        var detailsBox = new TextBox
        {
            Location = new Point(20, 132),
            Size = new Size(520, 220),
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            ReadOnly = true,
            Font = new Font("Consolas", 9f),
            Visible = false,
            Text = BuildDetails(report)
        };
        dialog.Controls.Add(detailsBox);

        var detailsButton = new Button
        {
            Text = "Details...",
            Location = new Point(360, 160),
            Size = new Size(90, 30)
        };
        dialog.Controls.Add(detailsButton);

        var closeButton = new Button
        {
            Text = "Close",
            Location = new Point(460, 160),
            Size = new Size(80, 30),
            DialogResult = DialogResult.OK
        };
        dialog.Controls.Add(closeButton);

        detailsButton.Click += (_, _) =>
        {
            var showDetails = !detailsBox.Visible;
            detailsBox.Visible = showDetails;
            detailsButton.Text = showDetails
                ? "Hide details"
                : "Details...";

            if (showDetails)
            {
                WindowLayout.SetClientSize(dialog, 560, 420);
                detailsButton.Location = WindowLayout.Point(dialog, 345, 370);
                closeButton.Location = WindowLayout.Point(dialog, 460, 370);
            }
            else
            {
                WindowLayout.SetClientSize(dialog, 560, 210);
                detailsButton.Location = WindowLayout.Point(dialog, 360, 160);
                closeButton.Location = WindowLayout.Point(dialog, 460, 160);
            }
        };

        dialog.AcceptButton = closeButton;
        dialog.CancelButton = closeButton;

        ThemeManager.Apply(dialog);

        WindowLayout.Prepare(dialog);
        dialog.ShowDialog(owner);
    }

    private static string BuildDetails(RuntimeToolHealthReport report)
    {
        var builder = new StringBuilder();

        foreach (var item in report.Items)
        {
            builder.AppendLine(
                $"{item.Name}: {FormatState(item.State)}");

            if (!string.IsNullOrWhiteSpace(item.InstalledVersion))
            {
                builder.AppendLine(
                    $"  Installed: {item.InstalledVersion}");
            }

            if (!string.IsNullOrWhiteSpace(item.LatestVersion))
            {
                builder.AppendLine(
                    $"  Latest:    {item.LatestVersion}");
            }

            builder.AppendLine($"  {item.Detail}");
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatState(RuntimeToolState state)
    {
        return state switch
        {
            RuntimeToolState.Missing => "Missing",
            RuntimeToolState.UpdateAvailable => "Update available",
            _ => "Ready"
        };
    }
}
