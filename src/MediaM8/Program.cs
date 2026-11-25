using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MediaM8;

internal static class Program
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    [STAThread]
    private static void Main(string[] commandLineArgs)
    {
        _ = SetCurrentProcessExplicitAppUserModelID("Zeekroket.MediaM8");

        // Render native controls at the system DPI instead of bitmap scaling.
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (commandLineArgs.Length > 0 && commandLineArgs[0] == "--apply-portable-update")
        {
            ApplicationUpdater.ApplyPortableUpdate(commandLineArgs);
            return;
        }

        using var logger = new SessionLogger(AppPaths.LogsDirectory);
        logger.Write($"Media M8 {AppInfo.Version} session started.");
        logger.Write($"Application path: {AppPaths.ApplicationDirectory}");
        logger.Write($"Data path: {AppPaths.DataDirectory}");
        logger.Write($"Mode: {(AppPaths.IsPortable ? "Portable" : "Installed")}");

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
