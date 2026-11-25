using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;

namespace MediaM8;

internal sealed record PreparedApplicationUpdate(
    string Directory,
    string FilePath,
    string Sha256,
    Version Version);

internal static class ApplicationUpdater
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/Zeekroket/Media-M8/releases/latest";
    private const string ReleaseDownloadPrefix =
        "https://github.com/Zeekroket/Media-M8/releases/download/";
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MediaM8/" + AppInfo.Version);
        return client;
    }

    public static async Task<PreparedApplicationUpdate> DownloadAsync(
        Version expectedVersion, CancellationToken cancellationToken)
    {
        using var response = await Client.GetAsync(LatestReleaseUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var release = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));
        var root = release.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (root.GetProperty("draft").GetBoolean() ||
            root.GetProperty("prerelease").GetBoolean() ||
            !Version.TryParse(tag.TrimStart('v', 'V'), out var version) ||
            version != expectedVersion ||
            version <= Version.Parse(AppInfo.Version))
        {
            throw new InvalidOperationException("The available release has changed. Check for updates again.");
        }

        var suffix = AppPaths.IsPortable ? "portable.zip" : "setup.exe";
        var name = $"MediaM8-{version}-win-x64-{suffix}";
        JsonElement asset = default;
        foreach (var candidate in root.GetProperty("assets").EnumerateArray())
        {
            if (candidate.GetProperty("name").GetString() == name)
            {
                asset = candidate;
                break;
            }
        }

        if (asset.ValueKind == JsonValueKind.Undefined ||
            !asset.TryGetProperty("digest", out var digestElement) ||
            digestElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("A verified update package is not available for this version.");
        }

        var digest = digestElement.GetString() ?? "";
        if (!digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ||
            !IsSha256(digest[7..]))
        {
            throw new InvalidOperationException("The release does not publish a valid SHA-256 checksum.");
        }

        var url = asset.GetProperty("browser_download_url").GetString() ?? "";
        if (!url.StartsWith(ReleaseDownloadPrefix, StringComparison.Ordinal) ||
            !url.EndsWith("/" + name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The update download address is unexpected.");
        }

        var directory = Path.Combine(
            Path.GetTempPath(), "MediaM8-update-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            var download = Path.Combine(directory, name);
            using (var fileResponse = await Client.GetAsync(
                url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                fileResponse.EnsureSuccessStatusCode();
                await using var input = await fileResponse.Content.ReadAsStreamAsync(cancellationToken);
                await using var output = new FileStream(download, FileMode.CreateNew);
                await input.CopyToAsync(output, cancellationToken);
            }

            var hash = await HashAsync(download, cancellationToken);
            if (!hash.Equals(digest[7..], StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The update checksum did not match. No files have been changed.");
            }

            if (!AppPaths.IsPortable)
            {
                return new PreparedApplicationUpdate(directory, download, hash, version);
            }

            // Extract only the executable, never overwrite portable data or extract arbitrary paths.
            var staged = Path.Combine(directory, "MediaM8.new.exe");
            using (var zip = ZipFile.OpenRead(download))
            {
                var entries = zip.Entries.Where(entry =>
                    entry.FullName.Equals("MediaM8.exe", StringComparison.OrdinalIgnoreCase)).ToList();
                if (entries.Count != 1 ||
                    !zip.Entries.Any(entry => entry.FullName == "portable.flag"))
                {
                    throw new InvalidDataException("The portable update package is invalid.");
                }
                entries[0].ExtractToFile(staged);
            }

            return new PreparedApplicationUpdate(
                directory, staged, await HashAsync(staged, cancellationToken), version);
        }
        catch
        {
            Cleanup(directory);
            throw;
        }
    }

    public static void Launch(PreparedApplicationUpdate update)
    {
        if (!AppPaths.IsPortable)
        {
            using var installer = Process.Start(new ProcessStartInfo(update.FilePath)
            {
                UseShellExecute = true
            }) ?? throw new InvalidOperationException("The installer could not be started.");
            return;
        }

        var target = Application.ExecutablePath;
        var helper = Path.Combine(update.Directory, "MediaM8-update-helper.exe");
        File.Copy(target, helper);
        var start = new ProcessStartInfo(helper) { UseShellExecute = false };
        start.ArgumentList.Add("--apply-portable-update");
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        start.ArgumentList.Add(target);
        start.ArgumentList.Add(update.FilePath);
        start.ArgumentList.Add(update.Sha256);
        using var process = Process.Start(start) ??
            throw new InvalidOperationException("The portable updater could not be started.");
    }

    public static void ApplyPortableUpdate(string[] args)
    {
        string? backup = null;
        string? localStage = null;
        var replaced = false;
        try
        {
            if (args.Length != 5 || !int.TryParse(args[1], out var pid) ||
                !IsSha256(args[4]))
            {
                throw new InvalidOperationException("Invalid update request.");
            }

            var target = Path.GetFullPath(args[2]);
            var folder = Path.GetDirectoryName(target)!;
            var staged = Path.GetFullPath(args[3]);
            if (!File.Exists(Path.Combine(folder, "portable.flag")) ||
                !File.Exists(target) ||
                !Path.GetFileName(staged).Equals("MediaM8.new.exe", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetDirectoryName(staged), AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The portable update paths are invalid.");
            }

            try
            {
                using var parent = Process.GetProcessById(pid);
                if (!parent.WaitForExit(60_000))
                {
                    throw new IOException("Media M8 is still open.");
                }
            }
            catch (ArgumentException)
            {
                // The original application has already exited.
            }

            if (!Hash(staged).Equals(args[4], StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The staged update checksum did not match.");
            }

            var token = Guid.NewGuid().ToString("N");
            localStage = Path.Combine(folder, ".MediaM8-update-" + token + ".exe");
            backup = Path.Combine(folder, ".MediaM8-backup-" + token + ".exe");
            File.Copy(staged, localStage);
            if (!Hash(localStage).Equals(args[4], StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The copied update checksum did not match.");
            }

            File.Replace(localStage, target, backup);
            replaced = true;
            using var restarted = Process.Start(new ProcessStartInfo(target)
            {
                UseShellExecute = true,
                WorkingDirectory = folder
            }) ?? throw new InvalidOperationException("Media M8 could not restart.");
            replaced = false;
            try { File.Delete(backup); } catch { /* Keep a recoverable backup. */ }
            MessageBox.Show("The portable update has been applied. Media M8 has been restarted.",
                "Update complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            var restored = false;
            if (replaced && backup is not null)
            {
                try
                {
                    File.Replace(backup, Path.GetFullPath(args[2]), null);
                    restored = true;
                }
                catch { /* Preserve the backup for manual recovery. */ }
            }
            var detail = replaced && !restored
                ? "\n\nA backup is available at:\n" + backup
                : "\n\nYour existing application files have been retained or restored.";
            MessageBox.Show("Media M8 could not apply the portable update.\n\n" + ex.Message + detail,
                "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (localStage is not null)
            {
                try { File.Delete(localStage); } catch { }
            }
        }
    }

    public static void Cleanup(string directory)
    {
        try { System.IO.Directory.Delete(directory, recursive: true); } catch { }
    }

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static async Task<string> HashAsync(string path, CancellationToken token)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, token));
    }
}
