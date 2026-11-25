namespace MediaM8;

internal static class AppPaths
{
    public static string ApplicationDirectory { get; } =
        AppContext.BaseDirectory;

    public static bool IsPortable { get; } =
        File.Exists(Path.Combine(ApplicationDirectory, "portable.flag"));

    public static string DataDirectory { get; } =
        IsPortable
            ? ApplicationDirectory
            : Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                AppInfo.Name);

    public static string ToolsDirectory =>
        Path.Combine(DataDirectory, "tools");

    public static string LogsDirectory =>
        Path.Combine(DataDirectory, "logs");

    public static string SettingsPath =>
        Path.Combine(DataDirectory, "settings.json");
}
