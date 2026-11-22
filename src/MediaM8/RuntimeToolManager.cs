using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MediaM8;

internal sealed record RuntimeToolPaths(
    string? YtDlp,
    string? FFmpeg,
    string? FFprobe,
    string? Deno);

internal static class RuntimeToolManager
{
    private const string YtDlpChecksumUrl =
        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS";
    private const string YtDlpUrl =
        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

    private const string FFmpegArchiveName = "ffmpeg-master-latest-win64-gpl.zip";
    private const string FFmpegChecksumUrl =
        "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/checksums.sha256";
    private const string FFmpegArchiveUrl =
        "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/" + FFmpegArchiveName;

    private const string DenoArchiveName = "deno-x86_64-pc-windows-msvc.zip";
    private const string DenoArchiveUrl =
        "https://github.com/denoland/deno/releases/latest/download/" + DenoArchiveName;
    private const string DenoChecksumUrl = DenoArchiveUrl + ".sha256sum";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static RuntimeToolPaths GetToolPaths(string toolsDirectory)
    {
        return new RuntimeToolPaths(
            FindLocalTool(toolsDirectory, "yt-dlp.exe"),
            FindLocalTool(toolsDirectory, "ffmpeg.exe"),
            FindLocalTool(toolsDirectory, "ffprobe.exe"),
            FindLocalTool(toolsDirectory, "deno.exe"));
    }

    public static IReadOnlyList<string> GetMissingTools(RuntimeToolPaths paths)
    {
        var missing = new List<string>();

        if (paths.YtDlp is null) missing.Add("yt-dlp");
        if (paths.FFmpeg is null) missing.Add("FFmpeg");
        if (paths.FFprobe is null) missing.Add("ffprobe");
        if (paths.Deno is null) missing.Add("Deno");

        return missing;
    }

    public static async Task InstallMissingToolsAsync(
        string toolsDirectory,
        SessionLogger logger,
        IProgress<string> progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(toolsDirectory);

        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "MediaM8-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDirectory);
        Report(logger, progress, $"Installer started. Target directory: {toolsDirectory}");

        try
        {
            var ytDlpTarget = Path.Combine(toolsDirectory, "yt-dlp.exe");
            if (!File.Exists(ytDlpTarget))
            {
                Report(logger, progress, "Starting yt-dlp installation.");

                var ytDlpChecksumFile = Path.Combine(tempDirectory, "yt-dlp-SHA2-256SUMS");
                await DownloadFileAsync(
                    YtDlpChecksumUrl,
                    ytDlpChecksumFile,
                    "yt-dlp checksum",
                    logger,
                    progress,
                    cancellationToken);

                var expected = GetChecksumForFile(ytDlpChecksumFile, "yt-dlp.exe");
                var ytDlpTemp = Path.Combine(tempDirectory, "yt-dlp.exe");

                await DownloadFileAsync(
                    YtDlpUrl,
                    ytDlpTemp,
                    "yt-dlp",
                    logger,
                    progress,
                    cancellationToken);

                ValidateSha256(ytDlpTemp, expected, "yt-dlp");
                File.Move(ytDlpTemp, ytDlpTarget, overwrite: true);
                Report(logger, progress, "yt-dlp installed and verified.");
            }

            var ffmpegTarget = Path.Combine(toolsDirectory, "ffmpeg.exe");
            var ffprobeTarget = Path.Combine(toolsDirectory, "ffprobe.exe");

            if (!File.Exists(ffmpegTarget) || !File.Exists(ffprobeTarget))
            {
                Report(logger, progress, "Starting FFmpeg/ffprobe installation.");

                var checksumFile = Path.Combine(tempDirectory, "ffmpeg-checksums.sha256");
                await DownloadFileAsync(
                    FFmpegChecksumUrl,
                    checksumFile,
                    "FFmpeg checksum",
                    logger,
                    progress,
                    cancellationToken);

                var expected = GetChecksumForFile(checksumFile, FFmpegArchiveName);
                var archivePath = Path.Combine(tempDirectory, FFmpegArchiveName);

                await DownloadFileAsync(
                    FFmpegArchiveUrl,
                    archivePath,
                    "FFmpeg archive",
                    logger,
                    progress,
                    cancellationToken);

                ValidateSha256(archivePath, expected, "FFmpeg archive");

                var extractDirectory = Path.Combine(tempDirectory, "ffmpeg");
                Report(logger, progress, "Extracting FFmpeg archive.");
                ZipFile.ExtractToDirectory(archivePath, extractDirectory, overwriteFiles: true);

                var ffmpegSource = FindFileRecursive(extractDirectory, "ffmpeg.exe");
                var ffprobeSource = FindFileRecursive(extractDirectory, "ffprobe.exe");

                if (ffmpegSource is null || ffprobeSource is null)
                {
                    throw new InvalidOperationException(
                        "FFmpeg archive did not contain ffmpeg.exe and ffprobe.exe.");
                }

                File.Copy(ffmpegSource, ffmpegTarget, overwrite: true);
                File.Copy(ffprobeSource, ffprobeTarget, overwrite: true);
                Report(logger, progress, "FFmpeg and ffprobe installed and verified.");
            }

            var denoTarget = Path.Combine(toolsDirectory, "deno.exe");
            if (!File.Exists(denoTarget))
            {
                Report(logger, progress, "Starting Deno installation.");

                var checksumFile = Path.Combine(tempDirectory, DenoArchiveName + ".sha256sum");
                await DownloadFileAsync(
                    DenoChecksumUrl,
                    checksumFile,
                    "Deno checksum",
                    logger,
                    progress,
                    cancellationToken);

                var checksumText = await File.ReadAllTextAsync(checksumFile, cancellationToken);
                var match = Regex.Match(checksumText, @"\b[a-fA-F0-9]{64}\b");
                if (!match.Success)
                {
                    throw new InvalidOperationException("No SHA-256 checksum was found for Deno.");
                }

                var archivePath = Path.Combine(tempDirectory, DenoArchiveName);
                await DownloadFileAsync(
                    DenoArchiveUrl,
                    archivePath,
                    "Deno archive",
                    logger,
                    progress,
                    cancellationToken);

                ValidateSha256(archivePath, match.Value, "Deno archive");

                var extractDirectory = Path.Combine(tempDirectory, "deno");
                Report(logger, progress, "Extracting Deno archive.");
                ZipFile.ExtractToDirectory(archivePath, extractDirectory, overwriteFiles: true);

                var denoSource = FindFileRecursive(extractDirectory, "deno.exe");
                if (denoSource is null)
                {
                    throw new InvalidOperationException("Deno archive did not contain deno.exe.");
                }

                File.Copy(denoSource, denoTarget, overwrite: true);
                Report(logger, progress, "Deno installed and verified.");
            }

            Report(logger, progress, "Required tools are ready.");
        }
        catch (Exception ex)
        {
            logger.Write($"Installer failed: {ex.Message}", "INSTALLER");
            throw;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
            catch (Exception cleanupException)
            {
                logger.Write(
                    $"Temporary installer directory could not be removed: {cleanupException.Message}",
                    "INSTALLER");
            }
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("Media-M8", AppInfo.Version));

        return client;
    }

    private static string? FindLocalTool(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        return File.Exists(path) ? Path.GetFullPath(path) : null;
    }

    private static string? FindFileRecursive(string directory, string fileName)
    {
        return Directory
            .EnumerateFiles(directory, fileName, SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    private static string GetChecksumForFile(string checksumFile, string fileName)
    {
        foreach (var line in File.ReadLines(checksumFile))
        {
            var trimmed = line.Trim();
            if (!trimmed.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var firstToken = trimmed
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (firstToken is not null &&
                Regex.IsMatch(firstToken, @"^[a-fA-F0-9]{64}$"))
            {
                return firstToken;
            }
        }

        throw new InvalidOperationException(
            $"No SHA-256 checksum was found for {fileName}.");
    }

    private static void ValidateSha256(
        string path,
        string expectedHash,
        string displayName)
    {
        using var stream = File.OpenRead(path);
        var actualHash = Convert.ToHexString(SHA256.HashData(stream));

        if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{displayName} integrity check failed.");
        }
    }

    private static async Task DownloadFileAsync(
        string url,
        string destination,
        string displayName,
        SessionLogger logger,
        IProgress<string> progress,
        CancellationToken cancellationToken)
    {
        Report(logger, progress, $"Downloading {displayName}...");

        using var response = await HttpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;

        await using var input = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var output = new FileStream(
            destination,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1024 * 1024,
            useAsync: true);

        var buffer = new byte[1024 * 1024];
        long downloadedBytes = 0;
        var lastReport = DateTime.UtcNow;

        while (true)
        {
            var read = await input
                .ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                .ConfigureAwait(false);

            if (read == 0)
            {
                break;
            }

            await output
                .WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                .ConfigureAwait(false);

            downloadedBytes += read;

            if ((DateTime.UtcNow - lastReport).TotalSeconds >= 2)
            {
                ReportDownloadProgress(
                    displayName,
                    downloadedBytes,
                    totalBytes,
                    logger,
                    progress);

                lastReport = DateTime.UtcNow;
            }
        }

        ReportDownloadProgress(
            displayName,
            downloadedBytes,
            totalBytes,
            logger,
            progress,
            completed: true);
    }

    private static void ReportDownloadProgress(
        string displayName,
        long downloadedBytes,
        long? totalBytes,
        SessionLogger logger,
        IProgress<string> progress,
        bool completed = false)
    {
        var downloadedMb = downloadedBytes / 1024d / 1024d;
        string message;

        if (completed)
        {
            message = $"{displayName} download completed: {downloadedMb:0.0} MB";
        }
        else if (totalBytes is > 0)
        {
            var totalMb = totalBytes.Value / 1024d / 1024d;
            var percent = downloadedBytes * 100d / totalBytes.Value;
            message =
                $"{displayName} progress: {downloadedMb:0.0} MB / {totalMb:0.0} MB ({percent:0.0}%)";
        }
        else
        {
            message = $"{displayName} progress: {downloadedMb:0.0} MB";
        }

        Report(logger, progress, message);
    }

    private static void Report(
        SessionLogger logger,
        IProgress<string> progress,
        string message)
    {
        logger.Write(message, "INSTALLER");
        progress.Report(message);
    }
}
