namespace MediaM8;

internal sealed class UpdateWorkspace : IDisposable
{
    private const string Prefix = "MediaM8-update-";
    private const string Marker = ".mediam8-update-owner";
    private const string Finished = ".mediam8-update-finished";
    private const string Magic = "Media M8 update workspace v1";
    private const string Helper = "MediaM8-update-helper.exe";
    private readonly FileStream _lease;

    private UpdateWorkspace(string directory, FileStream lease)
    {
        Directory = directory;
        _lease = lease;
    }

    public string Directory { get; }

    public static UpdateWorkspace Create(string tempRoot)
    {
        var directory = Path.Combine(tempRoot, Prefix + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        var marker = Path.Combine(directory, Marker);
        using (var stream = new FileStream(marker, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream))
            writer.Write(Magic);
        return Open(tempRoot, directory);
    }

    public static UpdateWorkspace Open(string tempRoot, string directory)
    {
        if (!IsOwnedPath(tempRoot, directory))
            throw new InvalidDataException("Invalid update workspace.");

        var lease = OpenMarker(directory, FileShare.Read);
        return new UpdateWorkspace(directory, lease);
    }

    public void Dispose() => _lease.Dispose();

    public void MarkFinished()
    {
        try
        {
            using var stream = new FileStream(Path.Combine(Directory, Finished),
                FileMode.CreateNew, FileAccess.Write);
        }
        catch { /* Later age-based cleanup can still recover the workspace. */ }
    }

    public static bool IsPackageName(string name, bool portable)
    {
        const string prefix = "MediaM8-";
        var suffix = portable ? "-win-x64-portable.zip" : "-win-x64-setup.exe";
        return name.StartsWith(prefix, StringComparison.Ordinal) &&
            name.EndsWith(suffix, StringComparison.Ordinal) &&
            name.Length > prefix.Length + suffix.Length &&
            Version.TryParse(name[prefix.Length..^suffix.Length], out _);
    }

    private static bool IsOwnedPath(string tempRoot, string directory)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(tempRoot));
        var name = Path.GetFileName(full);
        return string.Equals(Path.GetDirectoryName(full), root, StringComparison.OrdinalIgnoreCase) &&
            name.StartsWith(Prefix, StringComparison.Ordinal) &&
            Guid.TryParseExact(name[Prefix.Length..], "N", out _) &&
            (File.GetAttributes(full) & FileAttributes.ReparsePoint) == 0;
    }

    private static FileStream OpenMarker(string directory, FileShare share)
    {
        var path = Path.Combine(directory, Marker);
        if ((File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            throw new InvalidDataException("Invalid update workspace marker.");
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, share);
        try
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            if (stream.Length > 128 || reader.ReadToEnd() != Magic)
                throw new InvalidDataException("Invalid update workspace marker.");
            return stream;
        }
        catch { stream.Dispose(); throw; }
    }

    private static bool IsOwnedArtifact(string name) =>
        name is Helper or "MediaM8.new.exe" or Finished ||
        IsPackageName(name, portable: true) || IsPackageName(name, portable: false);

    public static void Cleanup(string tempRoot, string directory)
    {
        try
        {
            if (!IsOwnedPath(tempRoot, directory)) return;
            // Exclusive access means neither downloader nor helper is using it.
            using (OpenMarker(directory, FileShare.None))
            {
                foreach (var file in System.IO.Directory.EnumerateFiles(directory))
                {
                    if (Path.GetFileName(file) == Finished || !IsOwnedArtifact(Path.GetFileName(file))) continue;
                    if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) continue;
                    // A running helper's executable may still be locked.
                    try { File.Delete(file); } catch { }
                }
                if (System.IO.Directory.EnumerateFileSystemEntries(directory)
                    .Any(path => Path.GetFileName(path) is not (Marker or Finished)))
                    return;
            }
            File.Delete(Path.Combine(directory, Finished));
            File.Delete(Path.Combine(directory, Marker));
            System.IO.Directory.Delete(directory, recursive: false);
        }
        catch { /* Leave inaccessible or busy artifacts for a later run. */ }
    }

    public static void CleanupAbandoned(string tempRoot, DateTime utcNow)
    {
        try
        {
            foreach (var directory in System.IO.Directory.EnumerateDirectories(tempRoot, Prefix + "*"))
            {
                try
                {
                    if (!IsOwnedPath(tempRoot, directory)) continue;
                    var marker = Path.Combine(directory, Marker);
                    if (!File.Exists(marker)) continue;
                    // The grace period also protects the short gap between the
                    // downloader releasing its lease and the helper acquiring it.
                    if (File.Exists(Path.Combine(directory, Finished)) ||
                        utcNow - File.GetLastWriteTimeUtc(marker) >= TimeSpan.FromDays(1))
                        Cleanup(tempRoot, directory);
                }
                catch { }
            }
        }
        catch { }
    }
}
