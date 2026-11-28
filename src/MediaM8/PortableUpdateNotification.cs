namespace MediaM8;

internal static class PortableUpdateNotification
{
    internal const string StartupArgument = "--portable-update-complete";

    internal static bool ShouldShow(string[] args, bool isPortable) =>
        isPortable && args is [StartupArgument];
}
