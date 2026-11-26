namespace MediaM8;

internal enum DestinationProblem
{
    Unavailable,
    AccessDenied,
    DiskFull
}

internal sealed class DestinationFolderException : IOException
{
    public DestinationFolderException(string folder, DestinationProblem problem, Exception inner)
        : base($"Could not write to destination folder: {folder}", inner)
    {
        Problem = problem;
    }

    public DestinationProblem Problem { get; }
}

internal static class DestinationFolder
{
    public static void Validate(string folder, bool createIfMissing)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folder) || !Path.IsPathFullyQualified(folder))
                throw new ArgumentException("Choose an absolute destination folder.");

            if (createIfMissing)
                Directory.CreateDirectory(folder);

            if ((File.GetAttributes(folder) & FileAttributes.Directory) == 0)
                throw new IOException("The destination is not a directory.");

            // Only create our own unique temporary file. DeleteOnClose removes
            // it even if the write or flush fails, without touching user files.
            var probe = Path.Combine(folder, ".mediam8-write-" + Guid.NewGuid().ToString("N") + ".tmp");
            using var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 4096, FileOptions.DeleteOnClose);
            stream.WriteByte(0);
            stream.Flush(flushToDisk: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            throw new DestinationFolderException(folder, Classify(ex), ex);
        }
    }

    internal static DestinationProblem Classify(Exception exception)
    {
        // Win32 HRESULTs only; do not infer a full disk from an arbitrary IOException.
        var hresult = unchecked((uint)exception.HResult);
        if (hresult is 0x80070027u or 0x80070070u)
            return DestinationProblem.DiskFull;
        if (exception is UnauthorizedAccessException or System.Security.SecurityException ||
            hresult is 0x80070005u or 0x80070013u)
            return DestinationProblem.AccessDenied;

        return DestinationProblem.Unavailable;
    }
}
