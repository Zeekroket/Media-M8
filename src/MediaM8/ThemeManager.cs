using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace MediaM8;

internal enum AppTheme
{
    Light,
    Dark
}

internal static class ThemeManager
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int PbmSetBarColor = 0x0409;
    private const int PbmSetBkColor = 0x2001;

    private static readonly Color DarkWindow = Color.FromArgb(31, 31, 31);
    private static readonly Color DarkControl = Color.FromArgb(45, 45, 48);
    private static readonly Color DarkInput = Color.FromArgb(37, 37, 38);
    private static readonly Color DarkBorder = Color.FromArgb(70, 70, 74);
    private static readonly Color DarkText = Color.FromArgb(238, 238, 238);
    private static readonly Color DarkMutedText = Color.FromArgb(190, 190, 190);
    private static readonly Color DarkAccent = Color.FromArgb(0, 120, 215);

    private static readonly Color LightWindow = SystemColors.Control;
    private static readonly Color LightInput = SystemColors.Window;
    private static readonly Color LightText = SystemColors.ControlText;

    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Media M8");
    private static readonly string SettingsPath = Path.Combine(
        SettingsDirectory,
        "settings.json");

    public static event EventHandler? ThemeChanged;

    public static AppTheme CurrentTheme { get; private set; } =
        LoadSavedTheme();

    public static Color WindowBackColor =>
        CurrentTheme == AppTheme.Dark
            ? DarkWindow
            : LightWindow;

    public static Color InputBackColor =>
        CurrentTheme == AppTheme.Dark
            ? DarkInput
            : LightInput;

    public static Color TextColor =>
        CurrentTheme == AppTheme.Dark
            ? DarkText
            : LightText;

    public static Color SelectionBackColor =>
        CurrentTheme == AppTheme.Dark
            ? Color.FromArgb(62, 62, 66)
            : SystemColors.Highlight;

    public static Color SelectionTextColor =>
        CurrentTheme == AppTheme.Dark
            ? DarkText
            : SystemColors.HighlightText;

    public static Color BorderColor =>
        CurrentTheme == AppTheme.Dark
            ? DarkBorder
            : SystemColors.ControlDark;

    public static void SetTheme(AppTheme theme)
    {
        if (CurrentTheme == theme)
        {
            return;
        }

        CurrentTheme = theme;
        SaveTheme(theme);
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void Apply(Control root)
    {
        ApplyControl(root);

        foreach (Control child in root.Controls)
        {
            Apply(child);
        }

        if (root is Form form)
        {
            ApplyWindowChrome(form);
        }
    }

    public static void ApplyMenu(MenuStrip menu)
    {
        if (CurrentTheme == AppTheme.Dark)
        {
            menu.BackColor = DarkControl;
            menu.ForeColor = DarkText;
            menu.Renderer = new ToolStripProfessionalRenderer(
                new DarkColorTable());

            ApplyToolStripItems(
                menu.Items,
                DarkText);
        }
        else
        {
            menu.BackColor = SystemColors.MenuBar;
            menu.ForeColor = SystemColors.MenuText;
            menu.Renderer = new ToolStripProfessionalRenderer();

            ApplyToolStripItems(
                menu.Items,
                SystemColors.MenuText);
        }
    }

    public static void ApplyProgressBar(ProgressBar progressBar)
    {
        if (!progressBar.IsHandleCreated)
        {
            _ = progressBar.Handle;
        }

        if (CurrentTheme == AppTheme.Dark)
        {
            _ = SetWindowTheme(
                progressBar.Handle,
                string.Empty,
                string.Empty);
            _ = SendMessage(
                progressBar.Handle,
                PbmSetBkColor,
                IntPtr.Zero,
                ColorToWin32(DarkInput));
            _ = SendMessage(
                progressBar.Handle,
                PbmSetBarColor,
                IntPtr.Zero,
                ColorToWin32(DarkAccent));
        }
        else
        {
            _ = SetWindowTheme(
                progressBar.Handle,
                null,
                null);
            _ = SendMessage(
                progressBar.Handle,
                PbmSetBkColor,
                IntPtr.Zero,
                ColorToWin32(SystemColors.Control));
            _ = SendMessage(
                progressBar.Handle,
                PbmSetBarColor,
                IntPtr.Zero,
                ColorToWin32(SystemColors.Highlight));
        }
    }

    private static void ApplyControl(Control control)
    {
        var dark = CurrentTheme == AppTheme.Dark;

        switch (control)
        {
            case MenuStrip menu:
                ApplyMenu(menu);
                return;

            case TextBox textBox:
                textBox.BackColor = dark
                    ? DarkInput
                    : LightInput;
                textBox.ForeColor = dark
                    ? DarkText
                    : SystemColors.WindowText;
                textBox.BorderStyle = BorderStyle.Fixed3D;
                break;

            case RichTextBox richTextBox:
                richTextBox.BackColor = dark
                    ? DarkInput
                    : LightInput;
                richTextBox.ForeColor = dark
                    ? DarkText
                    : SystemColors.WindowText;
                break;

            case ComboBox comboBox:
                comboBox.BackColor = dark
                    ? DarkInput
                    : LightInput;
                comboBox.ForeColor = dark
                    ? DarkText
                    : SystemColors.WindowText;
                break;

            case ListView listView:
                listView.BackColor = dark
                    ? DarkInput
                    : LightInput;
                listView.ForeColor = dark
                    ? DarkText
                    : SystemColors.WindowText;
                break;

            case Button button:
                button.UseVisualStyleBackColor = !dark;
                button.FlatStyle = dark
                    ? FlatStyle.Flat
                    : FlatStyle.Standard;

                if (dark)
                {
                    button.BackColor = DarkControl;
                    button.ForeColor = DarkText;
                    button.FlatAppearance.BorderColor = DarkBorder;
                    button.FlatAppearance.MouseOverBackColor =
                        Color.FromArgb(58, 58, 62);
                    button.FlatAppearance.MouseDownBackColor =
                        Color.FromArgb(68, 68, 72);
                }
                else
                {
                    button.BackColor = LightWindow;
                    button.ForeColor = LightText;
                }

                break;

            case LinkLabel link:
                link.BackColor = dark
                    ? DarkWindow
                    : LightWindow;
                link.ForeColor = dark
                    ? DarkText
                    : LightText;
                link.LinkColor = dark
                    ? Color.FromArgb(90, 170, 255)
                    : Color.Blue;
                link.ActiveLinkColor = dark
                    ? Color.FromArgb(150, 205, 255)
                    : Color.Red;
                link.VisitedLinkColor = link.LinkColor;
                break;

            case GroupBox:
            case CheckBox:
            case RadioButton:
            case Label:
                control.BackColor = dark
                    ? DarkWindow
                    : LightWindow;
                control.ForeColor = dark
                    ? DarkText
                    : LightText;
                break;

            case PictureBox:
                control.BackColor = dark
                    ? DarkWindow
                    : LightWindow;
                break;

            case Panel:
                control.BackColor = dark
                    ? DarkWindow
                    : LightWindow;
                control.ForeColor = dark
                    ? DarkText
                    : LightText;
                break;

            case ProgressBar progress:
                ApplyProgressBar(progress);
                break;

            case Form:
                control.BackColor = dark
                    ? DarkWindow
                    : LightWindow;
                control.ForeColor = dark
                    ? DarkText
                    : LightText;
                break;

            default:
                control.BackColor = dark
                    ? DarkWindow
                    : LightWindow;
                control.ForeColor = dark
                    ? DarkText
                    : LightText;
                break;
        }
    }

    private static void ApplyToolStripItems(
        ToolStripItemCollection items,
        Color textColor)
    {
        foreach (ToolStripItem item in items)
        {
            item.ForeColor = textColor;

            if (item is ToolStripMenuItem menuItem)
            {
                ApplyToolStripItems(
                    menuItem.DropDownItems,
                    textColor);
            }
        }
    }

    private static void ApplyWindowChrome(Form form)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            var enabled =
                CurrentTheme == AppTheme.Dark
                    ? 1
                    : 0;

            _ = DwmSetWindowAttribute(
                form.Handle,
                DwmwaUseImmersiveDarkMode,
                ref enabled,
                Marshal.SizeOf<int>());
        }
        catch
        {
            // Window chrome theming is best effort only.
        }
    }

    private static AppTheme LoadSavedTheme()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return AppTheme.Light;
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<ThemeSettings>(json);

            return Enum.TryParse<AppTheme>(
                settings?.Appearance,
                ignoreCase: true,
                out var theme)
                ? theme
                : AppTheme.Light;
        }
        catch
        {
            return AppTheme.Light;
        }
    }

    private static void SaveTheme(AppTheme theme)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);

            var json = JsonSerializer.Serialize(
                new ThemeSettings(theme.ToString()),
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(
                SettingsPath,
                json);
        }
        catch
        {
            // Theme persistence must never prevent Media M8 from running.
        }
    }

    private static IntPtr ColorToWin32(Color color)
    {
        var value =
            color.R |
            (color.G << 8) |
            (color.B << 16);

        return new IntPtr(value);
    }

    private sealed record ThemeSettings(string Appearance);

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuStripGradientBegin => DarkControl;
        public override Color MenuStripGradientEnd => DarkControl;
        public override Color ToolStripDropDownBackground => DarkControl;
        public override Color ImageMarginGradientBegin => DarkControl;
        public override Color ImageMarginGradientMiddle => DarkControl;
        public override Color ImageMarginGradientEnd => DarkControl;
        public override Color MenuItemSelected => Color.FromArgb(62, 62, 66);
        public override Color MenuItemBorder => DarkBorder;
        public override Color MenuItemSelectedGradientBegin =>
            Color.FromArgb(62, 62, 66);
        public override Color MenuItemSelectedGradientEnd =>
            Color.FromArgb(62, 62, 66);
        public override Color MenuItemPressedGradientBegin =>
            Color.FromArgb(55, 55, 58);
        public override Color MenuItemPressedGradientMiddle =>
            Color.FromArgb(55, 55, 58);
        public override Color MenuItemPressedGradientEnd =>
            Color.FromArgb(55, 55, 58);
        public override Color ToolStripBorder => DarkBorder;
        public override Color SeparatorDark => DarkBorder;
        public override Color SeparatorLight => DarkBorder;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(
        IntPtr hwnd,
        string? subAppName,
        string? subIdList);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam);
}
