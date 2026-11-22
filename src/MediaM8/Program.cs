using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MediaM8;

internal static class Program
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    [STAThread]
    private static void Main()
    {
        _ = SetCurrentProcessExplicitAppUserModelID("Zeekroket.MediaM8");

        // Keep the original PowerShell POC layout dimensions consistent on
        // displays using Windows scaling. The POC was DPI-virtualized by its
        // host process, so Windows scaled the complete UI as one unit.
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var logger = new SessionLogger(AppContext.BaseDirectory);
        logger.Write($"Media M8 {AppInfo.Version} session started.");
        logger.Write($"Application path: {AppContext.BaseDirectory}");

        Application.ThreadException += (_, args) =>
        {
            logger.Write($"Unhandled UI exception: {args.Exception}", "ERROR");
            MessageBox.Show(
                $"Media M8 encountered an unexpected error.{Environment.NewLine}{Environment.NewLine}{args.Exception.Message}",
                AppInfo.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            logger.Write($"Unhandled exception: {args.ExceptionObject}", "ERROR");
        };

        logger.Write("Entering WinForms message loop.");
        Application.Run(new MainForm(logger));
        logger.Write("Media M8 session ended.");
    }
}

internal static class AppInfo
{
    public const string Name = "Media M8";
    public const string Version = "0.1.0";
}
