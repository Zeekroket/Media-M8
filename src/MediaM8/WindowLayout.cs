using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MediaM8;

// All layout coordinates are authored at 96 DPI.
internal static class WindowLayout
{
    private static readonly float TextScale = ReadTextScale();

    private sealed record LogicalControl(
        Control Control, Rectangle Bounds, Font Font, LogicalControl[] Children);

    private static float ReadTextScale()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Accessibility");
            if (key?.GetValue("TextScaleFactor") is int percent)
            {
                return Math.Clamp(percent, 100, 225) / 100f;
            }
        }
        catch
        {
            // Use standard text size if the setting cannot be read.
        }
        return 1f;
    }

    private static float Factor(Control control) =>
        control.DeviceDpi / 96f * TextScale;

    public static int Pixels(Control control, int logical) =>
        (int)Math.Round(logical * Factor(control));

    public static Point Point(Control control, int x, int y) =>
        new(Pixels(control, x), Pixels(control, y));

    public static Size Size(Control control, int width, int height) =>
        new(Pixels(control, width), Pixels(control, height));

    private static LogicalControl Capture(Control control) =>
        new(control, control.Bounds, control.Font,
            control.Controls.Cast<Control>().Select(Capture).ToArray());

    public static void Prepare(Form form, bool minimumClientSize = false)
    {
        form.AutoScaleMode = AutoScaleMode.None;
        // Capture the authored geometry before native handles and DPI-dependent
        // layout can adjust it. Do not scale the already laid-out Form twice.
        var logicalClientSize = form.ClientSize;
        var logicalMinimum = form.MinimumSize;
        var layout = Capture(form);

        form.Load += (_, _) =>
        {
            var factor = Factor(form);
            form.SuspendLayout();
            try
            {
                form.MinimumSize = System.Drawing.Size.Empty;
                form.ClientSize = Size(form, logicalClientSize.Width, logicalClientSize.Height);
                ApplyLayout(layout, factor, isRoot: true);

                form.MinimumSize = minimumClientSize
                    ? form.Size
                    : Size(form, logicalMinimum.Width, logicalMinimum.Height);

                FitToScreen(form);
            }
            finally
            {
                form.ResumeLayout(true);
            }
        };
    }

    private static void ApplyLayout(LogicalControl logical, float factor, bool isRoot = false)
    {
        var control = logical.Control;
        control.SuspendLayout();
        try
        {
            if (TextScale > 1f)
            {
                control.Font = new Font(
                    logical.Font.FontFamily, logical.Font.SizeInPoints * TextScale,
                    logical.Font.Style, GraphicsUnit.Point);
            }

            if (!isRoot && control.Dock == DockStyle.None)
            {
                var b = logical.Bounds;
                control.SetBounds(
                    (int)Math.Round(b.X * factor),
                    (int)Math.Round(b.Y * factor),
                    (int)Math.Round(b.Width * factor),
                    (int)Math.Round(b.Height * factor));
            }

            foreach (var child in logical.Children)
            {
                ApplyLayout(child, factor);
            }
        }
        finally
        {
            control.ResumeLayout(false);
        }
    }

    public static void FitToScreen(Form form)
    {
        var area = Screen.FromControl(form).WorkingArea;
        if (form.Width > area.Width || form.Height > area.Height)
        {
            form.AutoScroll = true;
            foreach (Control child in form.Controls)
            {
                if (child.Dock == DockStyle.None)
                {
                    child.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                }
            }

            form.MinimumSize = new Size(
                Math.Min(form.MinimumSize.Width, area.Width),
                Math.Min(form.MinimumSize.Height, area.Height));
            form.Size = new Size(
                Math.Min(form.Width, area.Width),
                Math.Min(form.Height, area.Height));
        }

        form.Location = new Point(
            Math.Clamp(form.Left, area.Left, Math.Max(area.Left, area.Right - form.Width)),
            Math.Clamp(form.Top, area.Top, Math.Max(area.Top, area.Bottom - form.Height)));
    }

    public static void SetClientSize(Form form, int width, int height, bool minimumClientSize = false)
    {
        form.AutoScrollPosition = new Point(0, 0);
        if (minimumClientSize)
        {
            form.MinimumSize = System.Drawing.Size.Empty;
        }
        form.ClientSize = Size(form, width, height);
        if (minimumClientSize)
        {
            form.MinimumSize = form.Size;
        }
        FitToScreen(form);
    }
}
