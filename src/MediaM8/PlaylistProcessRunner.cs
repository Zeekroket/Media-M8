using System.Diagnostics;

namespace MediaM8;

internal sealed record PlaylistProcessOutput(int ExitCode, string Stdout, string Stderr);

internal static class PlaylistProcessRunner
{
    public static async Task<PlaylistProcessOutput> RunAsync(
        ProcessStartInfo startInfo, CancellationToken cancellationToken,
        Action<Exception> onStopError)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException(
                "Could not start yt-dlp while preparing the playlist.");
        }

        // Cancellation runs the kill callback synchronously, including when
        // the main form closes. Wait for process exit and drain both pipes
        // before returning Stopped or starting the next queue item.
        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                // An exit racing with cancellation is harmless.
                try { if (process.HasExited) return; } catch { }
                onStopError(ex);
            }
        });

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return new PlaylistProcessOutput(process.ExitCode, stdout, stderr);
    }
}
