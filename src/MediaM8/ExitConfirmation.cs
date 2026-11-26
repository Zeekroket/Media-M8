namespace MediaM8;

internal static class ExitConfirmation
{
    public static string? GetMessage(bool active, int pending)
    {
        if (!active && pending == 0)
            return null;

        var lines = new List<string>();
        if (active)
            lines.Add("The active download or playlist preparation will be stopped.");
        if (pending > 0)
            lines.Add(pending == 1
                ? "The waiting download will be lost."
                : $"All {pending} waiting downloads will be lost.");

        return string.Join(Environment.NewLine, lines) +
            Environment.NewLine + Environment.NewLine + "Close Media M8?";
    }
}
