using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;

namespace MediaM8;

internal sealed record PreparedApplicationUpdate(
    UpdateWorkspace Workspace,
    string FilePath,
    string Sha256,
    Version Version)
{
    public string Directory => Workspace.Directory;
}

internal static class ApplicationUpdater
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/Zeekroket/Media-M8/releases/latest";
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
        var asset = UpdateAssetSelector.Select(
            release.RootElement, expectedVersion, Version.Parse(AppInfo.Version), AppPaths.IsPortable);
        var (name, url, digest, version) = asset;

        var workspace = UpdateWorkspace.Create(Path.GetTempPath());
        var directory = workspace.Directory;
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
            if (!hash.Equals(digest, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The update checksum did not match. No files have been changed.");
            }

            if (!AppPaths.IsPortable)
            {
                return new PreparedApplicationUpdate(workspace, download, hash, version);
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
                workspace, staged, await HashAsync(staged, cancellationToken), version);
        }
        catch
        {
            workspace.Dispose();
            Cleanup(directory);
            throw;
        }
    }

    public static void Launch(PreparedApplicationUpdate update)
    {
        var target = Application.ExecutablePath;
        var helper = Path.Combine(update.Directory, "MediaM8-update-helper.exe");
        File.Copy(target, helper);
        var start = new ProcessStartInfo(helper) { UseShellExecute = false };
        start.ArgumentList.Add(AppPaths.IsPortable ? "--apply-portable-update" : "--apply-installed-update");
        start.ArgumentList.Add(Environment.ProcessId.ToString());
        start.ArgumentList.Add(target);
        start.ArgumentList.Add(update.FilePath);
        start.ArgumentList.Add(update.Sha256);
        using var process = Process.Start(start) ??
            throw new InvalidOperationException("The update helper could not be started.");
    }

    private static void WaitForParent(int pid)
    {
        if (pid <= 0 || pid == Environment.ProcessId)
            throw new InvalidOperationException("Invalid update process.");
        try
        {
            using var parent = Process.GetProcessById(pid);
            if (!parent.WaitForExit(60_000))
                throw new IOException("Media M8 is still open.");
        }
        catch (ArgumentException)
        {
            // The original application has already exited.
        }
    }

    private static UpdateWorkspace OpenHelperWorkspace()
    {
        return UpdateWorkspace.Open(Path.GetTempPath(), AppContext.BaseDirectory);
    }

    private static bool IsWorkspaceFile(UpdateWorkspace workspace, string path) =>
        string.Equals(Path.GetDirectoryName(path),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(workspace.Directory)),
            StringComparison.OrdinalIgnoreCase) &&
        (File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) == 0;

    public static void ApplyInstalledUpdate(string[] args)
    {
        UpdateWorkspace? workspace = null;
        try
        {
            workspace = OpenHelperWorkspace();
            if (args.Length != 5 || !int.TryParse(args[1], out var pid) ||
                !UpdateAssetSelector.IsSha256(args[4]))
                throw new InvalidOperationException("Invalid update request.");

            var installerPath = Path.GetFullPath(args[3]);
            if (!IsWorkspaceFile(workspace, installerPath) ||
                !UpdateWorkspace.IsPackageName(Path.GetFileName(installerPath), portable: false))
                throw new InvalidOperationException("The installer path is invalid.");

            WaitForParent(pid);
            if (!Hash(installerPath).Equals(args[4], StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The installer checksum did not match.");

            using var installer = Process.Start(new ProcessStartInfo(installerPath)
            {
                UseShellExecute = true
            }) ?? throw new InvalidOperationException("The installer could not be started.");
            installer.WaitForExit();
            if (installer.ExitCode != 0)
            {
                MessageBox.Show("Setup did not complete. You can reopen Media M8 and try the update again.",
                    "Update not completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Media M8 could not complete the installer handoff. Reopen Media M8 and try again.\n\n" + ex.Message,
                "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            FinishWorkspace(workspace);
        }
    }

    private static void FinishWorkspace(UpdateWorkspace? workspace)
    {
        if (workspace is null) return;
        workspace.MarkFinished();
        workspace.Dispose();
        Cleanup(workspace.Directory);
    }

    public static void ApplyPortableUpdate(string[] args)
    {
        string? backup = null;
        string? localStage = null;
        var replaced = false;
        UpdateWorkspace? workspace = null;
        try
        {
            workspace = OpenHelperWorkspace();
            if (args.Length != 5 || !int.TryParse(args[1], out var pid) ||
                !UpdateAssetSelector.IsSha256(args[4]))
            {
                throw new InvalidOperationException("Invalid update request.");
            }

            var target = Path.GetFullPath(args[2]);
            var folder = Path.GetDirectoryName(target)!;
            var staged = Path.GetFullPath(args[3]);
            if (!File.Exists(Path.Combine(folder, "portable.flag")) ||
                !File.Exists(target) ||
                !Path.GetFileName(staged).Equals("MediaM8.new.exe", StringComparison.OrdinalIgnoreCase) ||
                !IsWorkspaceFile(workspace, staged))
            {
                throw new InvalidOperationException("The portable update paths are invalid.");
            }

            WaitForParent(pid);

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
                WorkingDirectory = folder,
                Arguments = PortableUpdateNotification.StartupArgument
            }) ?? throw new InvalidOperationException("Media M8 could not restart.");
            replaced = false;
            try { File.Delete(backup); } catch { /* Keep a recoverable backup. */ }
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
            var detail = backup is not null && File.Exists(backup)
                ? "\n\nA recovery backup is available at:\n" + backup
                : restored
                    ? "\n\nThe original executable has been restored."
                    : "\n\nCheck the existing executable before retrying. Your tools, logs and settings have not been changed.";
            MessageBox.Show("Media M8 could not apply the portable update.\n\n" + ex.Message + detail,
                "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (localStage is not null)
            {
                try { File.Delete(localStage); } catch { }
            }
            FinishWorkspace(workspace);
        }
    }

    public static void Cleanup(string directory) =>
        UpdateWorkspace.Cleanup(Path.GetTempPath(), directory);

    public static void CleanupAbandoned() =>
        UpdateWorkspace.CleanupAbandoned(Path.GetTempPath(), DateTime.UtcNow);

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
