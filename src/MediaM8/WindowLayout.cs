using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MediaM8;

// Coordinates in the existing forms are authored at 96 DPI.
// System-aware WinForms controls render text at the actual system DPI.
internal static class WindowLayout
{
    private static readonly float TextScale = ReadTextScale();

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

    public static void Prepare(Form form)
    {
        form.AutoScaleMode = AutoScaleMode.None;
        form.Load += (_, _) =>
        {
            form.SuspendLayout();
            try
            {
                var factor = Factor(form);
                if (Math.Abs(factor - 1f) > 0.001f)
                {
                    form.Scale(new SizeF(factor, factor));
                }

                if (TextScale > 1f)
                {
                    // Capture fonts before changing the parent's inherited font.
                    var controls = new List<(Control Control, Font Font)>();
                    CollectFonts(form, controls);
                    foreach (var (control, font) in controls)
                    {
                        control.Font = new Font(
                            font.FontFamily, font.SizeInPoints * TextScale,
                            font.Style, GraphicsUnit.Point);
                    }
                }

                FitToScreen(form);
            }
            finally
            {
                form.ResumeLayout(true);
            }
        };
    }

    private static void CollectFonts(
        Control control, List<(Control Control, Font Font)> fonts)
    {
        fonts.Add((control, control.Font));
        foreach (Control child in control.Controls)
        {
            CollectFonts(child, fonts);
        }
    }

    public static void FitToScreen(Form form)
    {
        var area = Screen.FromControl(form).WorkingArea;
        if (form.Width <= area.Width && form.Height <= area.Height)
        {
            return;
        }

        // Keep the content accessible when scaling exceeds the work area.
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
        form.Location = new Point(
            Math.Clamp(form.Left, area.Left, Math.Max(area.Left, area.Right - form.Width)),
            Math.Clamp(form.Top, area.Top, Math.Max(area.Top, area.Bottom - form.Height)));
    }

    public static void SetClientSize(Form form, int width, int height)
    {
        form.AutoScrollPosition = Point(form, 0, 0);
        form.ClientSize = Size(form, width, height);
        FitToScreen(form);
    }
}
