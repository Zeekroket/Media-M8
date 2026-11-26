using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaM8;

// Local child-process fixtures; no yt-dlp or network access required.
if (args is ["--playlist-fixture"])
{
    Console.WriteLine("fixture stdout");
    Console.Error.WriteLine("fixture stderr");
    return 0;
}
if (args is ["--playlist-fixture-wait", var readyPath])
{
    File.WriteAllText(readyPath, Environment.ProcessId.ToString());
    Thread.Sleep(TimeSpan.FromSeconds(30));
    return 0;
}

var tests = new (string Name, Action Run)[]
{
    ("Progress: bytes, speed and ETA", () =>
    {
        var p = Progress("download", """{"status":"downloading","downloaded_bytes":250,"total_bytes":1000,"speed":125,"eta":6}""");
        Equal(DownloadPhase.Downloading, p.Phase);
        Equal<double?>(25, p.Percentage);
        Equal<long?>(250, p.DownloadedBytes);
        Equal<long?>(1000, p.TotalBytes);
        Equal<double?>(125, p.SpeedBytesPerSecond);
        Equal<int?>(6, p.EtaSeconds);
    }),
    ("Progress: estimated bytes", () => Equal<double?>(50, Progress("download", """{"downloaded_bytes":500,"total_bytes_estimate":1000}""").Percentage)),
    ("Progress: fragments", () => Equal<double?>(30, Progress("download", """{"fragment_index":3,"fragment_count":10}""").Percentage)),
    ("Progress: fallback percentage clamped", () => Equal<double?>(100, Progress("download", """{"_percent":120}""").Percentage)),
    ("Progress: missing totals", () => Equal<double?>(null, Progress("download", """{"status":"downloading","downloaded_bytes":50}""").Percentage)),
    ("Progress: finished and post-processing", () =>
    {
        Equal<double?>(100, Progress("download", """{"status":"finished"}""").Percentage);
        Equal(DownloadPhase.Processing, Progress("postprocess", """{"status":"started"}""").Phase);
    }),
    ("Progress: unrelated or malformed lines", () =>
    {
        Equal(false, DownloadProgressParser.TryParse("ordinary log output", out _));
        Equal(false, DownloadProgressParser.TryParse(DownloadProgressParser.Marker + "download|{broken", out _));
    }),
    ("Queue: FIFO and state", () =>
    {
        var q = new DownloadQueueModel();
        var a = q.Enqueue(Request("a"));
        var b = q.Enqueue(Request("b"));
        Equal(2, q.PendingCount);
        Equal(a, q.GetNextPending());
        q.SetState(a, DownloadQueueState.Downloading);
        Equal(b, q.GetNextPending());
        q.SetState(a, DownloadQueueState.Completed, "done");
        Equal("done", a.StatusDetail);
        Equal(1, q.PendingCount);
    }),
    ("Queue: reorder pending, preserve active", () =>
    {
        var q = new DownloadQueueModel();
        var a = q.Enqueue(Request("a"));
        var b = q.Enqueue(Request("b"));
        var c = q.Enqueue(Request("c"));
        q.SetState(a, DownloadQueueState.Downloading);
        Equal(false, q.MovePending(a.Id, 1));
        Equal(false, q.CanMovePending(b.Id, -1));
        Equal(true, q.MovePending(c.Id, -1));
        Equal(c, q.GetNextPending());
        Equal(a, q.Items[0]);
        Equal(true, q.MovePending(c.Id, 1));
        Equal(b, q.GetNextPending());
    }),
    ("Queue: retry only failed/stopped", () =>
    {
        var q = new DownloadQueueModel();
        var a = q.Enqueue(Request("a"));
        Equal<DownloadQueueItem?>(null, q.Retry(a.Id));
        q.SetState(a, DownloadQueueState.Failed);
        var retry = q.Retry(a.Id) ?? throw new Exception("Missing retry.");
        Equal(a.Request, retry.Request);
        Equal(true, retry.Id != a.Id);
        Equal(DownloadQueueState.Pending, retry.State);
        Equal(DownloadQueueState.Failed, a.State);
        q.SetState(retry, DownloadQueueState.Stopped);
        Equal(true, q.Retry(retry.Id) is not null);
        q.SetState(a, DownloadQueueState.Completed);
        Equal<DownloadQueueItem?>(null, q.Retry(a.Id));
    }),
    ("Queue: remove, clear and pause", () =>
    {
        var q = new DownloadQueueModel();
        var a = q.Enqueue(Request("a"));
        var b = q.Enqueue(Request("b"));
        var c = q.Enqueue(Request("c"));
        var d = q.Enqueue(Request("d"));
        q.SetState(a, DownloadQueueState.Completed);
        q.SetState(b, DownloadQueueState.Downloading);
        Equal(false, q.RemovePending(b.Id));
        Equal(true, q.RemovePending(c.Id));
        Equal(1, q.ClearFinished());
        Equal(2, q.Items.Count);
        Equal(d, q.GetNextPending());
        q.SetPaused(true);
        Equal(true, q.IsPaused);
        q.SetPaused(false);
        Equal(false, q.IsPaused);
    }),
    ("Archive: exact skip messages, including title", () =>
    {
        var tracker = new ArchiveSkipTracker();
        tracker.Observe("[download] fixture-id: has already been recorded in the archive");
        tracker.Observe("[download] fixture-id: Fixture title has already been recorded in the archive");
        for (var i = 0; i < 40; i++) tracker.Observe("[debug] fixture diagnostic");
        Equal(2, tracker.ArchiveSkipCount);
        Equal(true, tracker.IsArchiveOnly(true, 0));
        Equal(false, tracker.IsArchiveOnly(false, 0));
        Equal(false, tracker.IsArchiveOnly(true, 1));
    }),
    ("Archive: ordinary existing file and unrelated output are not archive skips", () =>
    {
        var tracker = new ArchiveSkipTracker();
        tracker.Observe("[download] fixture.mp3 has already been downloaded");
        tracker.Observe("WARNING: has already been recorded in the archive");
        tracker.Observe("[download] has already been recorded in the archive but more text follows");
        Equal(0, tracker.ArchiveSkipCount);
        Equal(false, tracker.IsArchiveOnly(true, 0));
    }),
    ("Archive: progress and skip in one process", () =>
    {
        var tracker = new ArchiveSkipTracker();
        tracker.Observe("[download] fixture-id: has already been recorded in the archive");
        tracker.Observe(DownloadProgressParser.Marker + "download|" + """{"status":"finished"}""");
        Equal(1, tracker.ArchiveSkipCount);
        Equal(false, tracker.IsArchiveOnly(true, 0));
    }),
    ("Archive: fully skipped, normal and mixed plans", () =>
    {
        var skipped = new DownloadItemResults();
        skipped.Record(true, 1);
        skipped.Record(true, 1);
        Equal(DownloadQueueState.Skipped, skipped.State);
        Equal(0, skipped.Completed);
        Equal(2, skipped.Skipped);
        var mixed = new DownloadItemResults();
        mixed.Record(false, 0);
        mixed.Record(true, 1);
        mixed.Record(false, 0);
        Equal(DownloadQueueState.CompletedWithSkips, mixed.State);
        Equal("2 completed, 1 skipped", mixed.Detail);
        var normal = new DownloadItemResults();
        normal.Record(false, 0);
        Equal(DownloadQueueState.Completed, normal.State);
    }),
    ("Archive: skip results are terminal and not retryable", () =>
    {
        var q = new DownloadQueueModel();
        var a = q.Enqueue(Request("a"));
        var b = q.Enqueue(Request("b"));
        var c = q.Enqueue(Request("c"));
        q.SetState(a, DownloadQueueState.Skipped);
        q.SetState(b, DownloadQueueState.CompletedWithSkips);
        Equal<DownloadQueueItem?>(null, q.Retry(a.Id));
        Equal<DownloadQueueItem?>(null, q.Retry(b.Id));
        Equal(c, q.GetNextPending());
        Equal(2, q.ClearFinished());
        Equal(1, q.Items.Count);
    }),
    ("Failure: known VK extractor error", () =>
    {
        const string log = "[vk] fixture: Downloading MPD manifest\nERROR: 'NoneType' object has no attribute 'split'";
        var exception = new DownloadFailureException("The download process failed.", log);
        var failure = FriendlyDownloadFailure.From(exception);
        Equal("Download tool problem", failure.Title);
        Equal("Download tool problem.", failure.Status);
        Equal(true, failure.NextAction.Contains("Update download tools", StringComparison.Ordinal));
        Equal(log, exception.TechnicalDetails);
    }),
    ("Failure: VK traceback evidence", () =>
    {
        var exception = new DownloadFailureException("The download process failed.",
            "File \"C:/fixture/yt_dlp/extractor/vk.py\", line 100\nAttributeError: 'NoneType' object has no attribute 'split'");
        Equal("Download tool problem", FriendlyDownloadFailure.From(exception).Title);
    }),
    ("Failure: unknown exception or unrelated output keeps generic message", () =>
    {
        const string error = "ERROR: 'NoneType' object has no attribute 'split'";
        Equal("Download could not be completed", FriendlyDownloadFailure.From(new DownloadFailureException("Failed.", "[youtube] fixture\n" + error)).Title);
        Equal("Download could not be completed", FriendlyDownloadFailure.From(new DownloadFailureException("Failed.", "[vk] fixture\nERROR: 'NoneType' object has no attribute 'other'")).Title);
        Equal("Download could not be completed", FriendlyDownloadFailure.From(new DownloadFailureException("Failed.", "[vk] fixture\nWARNING: " + error)).Title);
        Equal("Download could not be completed", FriendlyDownloadFailure.From(new InvalidOperationException("[vk] fixture\n" + error)).Title);
    }),
    ("Failure: existing private, access, connection and disk messages remain", () =>
    {
        foreach (var (details, expected) in new[]
        {
            ("ERROR: Private video", "This media is private"),
            ("ERROR: login required", "Sign-in is required"),
            ("ERROR: video unavailable", "This media is not available"),
            ("ERROR: Unsupported URL", "This link is not supported"),
            ("ERROR: Connection refused", "Connection problem"),
            ("ERROR: No space left on device", "Not enough free space")
        })
            Equal(expected, FriendlyDownloadFailure.From(new DownloadFailureException("Failed.", details)).Title);
    }),
    ("Playlist process: normal exit drains both output streams", () =>
    {
        var output = PlaylistProcessRunner.RunAsync(
            PlaylistFixture("--playlist-fixture"), CancellationToken.None,
            ex => throw new Exception("Unexpected cancellation failure.", ex))
            .WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        Equal(0, output.ExitCode);
        Equal("fixture stdout", output.Stdout.Trim());
        Equal("fixture stderr", output.Stderr.Trim());
    }),
    ("Playlist process: cancellation before start", () =>
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        // The deliberately invalid executable must never be started.
        var start = new ProcessStartInfo("nonexistent-media-m8-fixture-executable");
        Throws<OperationCanceledException>(() =>
            PlaylistProcessRunner.RunAsync(start, cts.Token, _ => { })
                .GetAwaiter().GetResult());
    }),
    ("Playlist process: STOP terminates active child and drains pipes", () =>
    {
        var directory = Path.Combine(Path.GetTempPath(), "MediaM8-playlist-fixture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var readyPath = Path.Combine(directory, "ready");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Exception? stopError = null;
        var task = PlaylistProcessRunner.RunAsync(
            PlaylistFixture("--playlist-fixture-wait", readyPath), cts.Token,
            ex => stopError = ex);
        int pid = 0;
        try
        {
            var timer = Stopwatch.StartNew();
            while (timer.Elapsed < TimeSpan.FromSeconds(10) && !task.IsCompleted)
            {
                if (File.Exists(readyPath) && int.TryParse(File.ReadAllText(readyPath), out pid))
                    break;
                Thread.Sleep(25);
            }
            if (pid <= 0)
                throw new Exception("The fixture did not start.");
            cts.Cancel();
            Throws<OperationCanceledException>(() =>
                task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult());
            Equal<Exception?>(null, stopError);
            try
            {
                using var child = Process.GetProcessById(pid);
                Equal(true, child.HasExited);
            }
            catch (ArgumentException)
            {
                // The child no longer exists.
            }
        }
        finally
        {
            cts.Cancel();
            try { task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { }
            catch (TimeoutException)
            {
                if (pid > 0)
                {
                    try
                    {
                        using var child = Process.GetProcessById(pid);
                        if (!child.HasExited) child.Kill(entireProcessTree: true);
                        child.WaitForExit(5000);
                    }
                    catch (ArgumentException) { }
                }
            }
            finally { Directory.Delete(directory, recursive: true); }
        }
    }),
    ("Exit: idle and finished-only queue needs no confirmation", () =>
    {
        var queue = new DownloadQueueModel();
        Equal<string?>(null, ExitConfirmation.GetMessage(false, queue.PendingCount));
        var item = queue.Enqueue(Request("finished"));
        queue.SetState(item, DownloadQueueState.Completed);
        Equal<string?>(null, ExitConfirmation.GetMessage(false, queue.PendingCount));
    }),
    ("Exit: active preparation or download requires confirmation", () =>
    {
        var warning = ExitConfirmation.GetMessage(true, 0) ?? throw new Exception("Missing warning.");
        Equal(true, warning.Contains("active download or playlist preparation will be stopped", StringComparison.Ordinal));
        Equal(false, warning.Contains("waiting download", StringComparison.Ordinal));
    }),
    ("Exit: paused pending queue still requires confirmation", () =>
    {
        var queue = new DownloadQueueModel();
        queue.Enqueue(Request("waiting"));
        queue.SetPaused(true);
        var warning = ExitConfirmation.GetMessage(false, queue.PendingCount) ?? throw new Exception("Missing warning.");
        Equal(true, warning.Contains("The waiting download will be lost.", StringComparison.Ordinal));
        Equal(false, warning.Contains("active download", StringComparison.Ordinal));
        Equal(1, queue.PendingCount);
        Equal(true, queue.IsPaused);
    }),
    ("Exit: active and multiple pending items warn about both", () =>
    {
        var warning = ExitConfirmation.GetMessage(true, 3) ?? throw new Exception("Missing warning.");
        Equal(true, warning.Contains("active download or playlist preparation will be stopped", StringComparison.Ordinal));
        Equal(true, warning.Contains("All 3 waiting downloads will be lost.", StringComparison.Ordinal));
    }),
    ("Destination: create new folder, verify write access and preserve existing files", () =>
    {
        WithSettings((_, path) =>
        {
            var folder = Path.Combine(Path.GetDirectoryName(path)!, "downloads");
            DestinationFolder.Validate(folder, createIfMissing: true);
            Equal(true, Directory.Exists(folder));
            Equal(0, Directory.GetFiles(folder).Length);
            var existing = Path.Combine(folder, "existing.txt");
            File.WriteAllText(existing, "keep");
            DestinationFolder.Validate(folder, createIfMissing: false);
            Equal("keep", File.ReadAllText(existing));
            Equal(1, Directory.GetFiles(folder).Length);
        });
    }),
    ("Destination: missing queued folder is not recreated", () =>
    {
        WithSettings((_, path) =>
        {
            var folder = Path.Combine(Path.GetDirectoryName(path)!, "missing");
            DestinationFolder.Validate(folder, createIfMissing: true);
            Directory.Delete(folder);
            Throws<DestinationFolderException>(() => DestinationFolder.Validate(folder, createIfMissing: false));
            Equal(false, Directory.Exists(folder));
        });
    }),
    ("Destination: reject a file or relative path", () =>
    {
        WithSettings((_, path) =>
        {
            File.WriteAllText(path, "keep");
            Throws<DestinationFolderException>(() => DestinationFolder.Validate(path, createIfMissing: true));
            Throws<DestinationFolderException>(() => DestinationFolder.Validate("relative-fixture", createIfMissing: true));
            Equal("keep", File.ReadAllText(path));
        });
    }),
    ("Destination: classify exact filesystem HRESULTs", () =>
    {
        Equal(DestinationProblem.AccessDenied, DestinationFolder.Classify(new UnauthorizedAccessException()));
        Equal(DestinationProblem.AccessDenied, DestinationFolder.Classify(new IOException("write protected", unchecked((int)0x80070013))));
        Equal(DestinationProblem.DiskFull, DestinationFolder.Classify(new IOException("disk full", unchecked((int)0x80070070))));
        Equal(DestinationProblem.DiskFull, DestinationFolder.Classify(new IOException("disk full", unchecked((int)0x80070027))));
        Equal(DestinationProblem.Unavailable, DestinationFolder.Classify(new DirectoryNotFoundException()));
        Equal(DestinationProblem.Unavailable, DestinationFolder.Classify(new IOException("unknown")));
    }),
    ("Destination: validation failures preserve path and inner exception", () =>
    {
        var inner = new UnauthorizedAccessException("fixture permission error");
        var error = new DestinationFolderException("fixture-folder", DestinationProblem.AccessDenied, inner);
        Equal("Cannot write to download folder", FriendlyDownloadFailure.From(error).Title);
        Equal(true, error.ToString().Contains("fixture-folder", StringComparison.Ordinal));
        Equal<Exception?>(inner, error.InnerException);
        Equal("Download folder unavailable", FriendlyDownloadFailure.From(new DestinationFolderException("fixture", DestinationProblem.Unavailable, new IOException())).Title);
        Equal("Not enough free space", FriendlyDownloadFailure.From(new DestinationFolderException("fixture", DestinationProblem.DiskFull, new IOException())).Title);
    }),
    ("Destination: yt-dlp and ffmpeg filesystem failures", () =>
    {
        foreach (var (details, title) in new[]
        {
            ("ERROR: unable to open for writing: [Errno 13] Permission denied", "Cannot write to download folder"),
            ("ERROR: Postprocessing: output.mp3: Permission denied", "Cannot write to download folder"),
            ("ERROR: [WinError 5] Access is denied", "Cannot write to download folder"),
            ("ERROR: Postprocessing: No space left on device", "Not enough free space"),
            ("ERROR: [WinError 112] There is not enough space on the disk", "Not enough free space"),
            ("ERROR: unable to open for writing: [Errno 2] No such file or directory", "Download folder unavailable"),
            ("ERROR: unable to create directory: [WinError 21] The device is not ready", "Download folder unavailable"),
            ("ERROR: HTTP Error 403: Forbidden", "Download could not be completed"),
            ("ERROR: HTTP Error 403: Access denied", "Download could not be completed")
        })
            Equal(title, FriendlyDownloadFailure.From(new DownloadFailureException("Failed.", details)).Title);
    }),
    ("Settings: missing and invalid values", () => WithSettings((store, path) =>
    {
        Equal<string?>(null, store.Read("Appearance"));
        File.WriteAllText(path, """{"Appearance":42,"DownloadMode":"Video"}""");
        Equal<string?>(null, store.Read("Appearance"));
        Equal("Video", store.Read("DownloadMode"));
        File.WriteAllText(path, "{broken");
        Equal<string?>(null, store.Read("DownloadMode"));
        store.Write(("DownloadMode", "MP3"));
        Equal("MP3", store.Read("DownloadMode"));
        File.WriteAllText(path, "[]");
        Equal<string?>(null, store.Read("DownloadMode"));
    })),
    ("Settings: preserve other keys and reopen", () => WithSettings((store, path) =>
    {
        File.WriteAllText(path, """{"Appearance":"Dark","FutureKey":{"a":1}}""");
        store.Write(("DownloadFolder", "fixture-folder"), ("AudioQuality", "320 kbps"));
        store.Write(("Appearance", "Light"));
        var reopened = new SettingsStore(path);
        Equal("Light", reopened.Read("Appearance"));
        Equal("fixture-folder", reopened.Read("DownloadFolder"));
        Equal("320 kbps", reopened.Read("AudioQuality"));
        Equal(1, JsonNode.Parse(File.ReadAllText(path))!["FutureKey"]!["a"]!.GetValue<int>());
        Equal(1, Directory.GetFiles(Path.GetDirectoryName(path)!).Length);
    })),
    ("Update workspace: cancel cleans only owned artifacts", () =>
    {
        WithSettings((_, path) =>
        {
            var root = Path.GetDirectoryName(path)!;
            string directory;
            using (var workspace = UpdateWorkspace.Create(root))
            {
                directory = workspace.Directory;
                File.WriteAllText(Path.Combine(directory, "MediaM8-0.2.1-win-x64-portable.zip"), "fixture");
                File.WriteAllText(Path.Combine(directory, "MediaM8.new.exe"), "fixture");
                File.WriteAllText(Path.Combine(directory, "MediaM8-update-helper.exe"), "fixture");
            }
            UpdateWorkspace.Cleanup(root, directory);
            Equal(false, Directory.Exists(directory));
        });
    }),
    ("Update workspace: active leases block cleanup", () =>
    {
        WithSettings((_, path) =>
        {
            var root = Path.GetDirectoryName(path)!;
            string directory;
            using (var workspace = UpdateWorkspace.Create(root))
            {
                directory = workspace.Directory;
                using var helper = UpdateWorkspace.Open(root, directory);
                var asset = Path.Combine(directory, "MediaM8-0.2.1-win-x64-setup.exe");
                File.WriteAllText(asset, "fixture");
                workspace.MarkFinished();
                UpdateWorkspace.Cleanup(root, directory);
                Equal(true, File.Exists(asset));
                UpdateWorkspace.CleanupAbandoned(root, DateTime.UtcNow.AddDays(2));
                Equal(true, File.Exists(asset));
            }
            UpdateWorkspace.CleanupAbandoned(root, DateTime.UtcNow);
            Equal(false, Directory.Exists(directory));
        });
    }),
    ("Update workspace: young handoff preserved, abandoned workspace removed", () =>
    {
        WithSettings((_, path) =>
        {
            var root = Path.GetDirectoryName(path)!;
            string directory;
            using (var workspace = UpdateWorkspace.Create(root))
                directory = workspace.Directory;
            var now = DateTime.UtcNow;
            UpdateWorkspace.CleanupAbandoned(root, now);
            Equal(true, Directory.Exists(directory));
            UpdateWorkspace.CleanupAbandoned(root, now.AddDays(2));
            Equal(false, Directory.Exists(directory));
        });
    }),
    ("Update workspace: unrelated directories, unknown files and backups preserved", () =>
    {
        WithSettings((_, path) =>
        {
            var root = Path.GetDirectoryName(path)!;
            var unrelated = Path.Combine(root, "MediaM8-update-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(unrelated);
            var untouched = Path.Combine(unrelated, "MediaM8.new.exe");
            File.WriteAllText(untouched, "unmarked");
            string directory;
            using (var workspace = UpdateWorkspace.Create(root))
            {
                directory = workspace.Directory;
                File.WriteAllText(Path.Combine(directory, "notes.txt"), "keep");
                File.WriteAllText(Path.Combine(directory, ".MediaM8-backup-fixture.exe"), "backup");
                File.WriteAllText(Path.Combine(directory, "MediaM8.new.exe"), "owned");
                workspace.MarkFinished();
            }
            UpdateWorkspace.CleanupAbandoned(root, DateTime.UtcNow.AddDays(2));
            Equal(true, File.Exists(untouched));
            Equal("keep", File.ReadAllText(Path.Combine(directory, "notes.txt")));
            Equal("backup", File.ReadAllText(Path.Combine(directory, ".MediaM8-backup-fixture.exe")));
            Equal(false, File.Exists(Path.Combine(directory, "MediaM8.new.exe")));
        });
    }),
    ("Update workspace: package names must match distribution and version", () =>
    {
        Equal(true, UpdateWorkspace.IsPackageName("MediaM8-0.2.1-win-x64-setup.exe", false));
        Equal(true, UpdateWorkspace.IsPackageName("MediaM8-0.2.1-win-x64-portable.zip", true));
        Equal(false, UpdateWorkspace.IsPackageName("MediaM8-0.2.1-win-x64-portable.zip", false));
        Equal(false, UpdateWorkspace.IsPackageName("MediaM8-not-a-version-win-x64-setup.exe", false));
    }),
    ("Update: newer, equal and older versions", () =>
    {
        using var d = JsonDocument.Parse(Release());
        Equal(UpdateAvailability.NewVersionAvailable, UpdateChecker.ParseRelease(d.RootElement, new Version(0, 2, 0)).Availability);
        Equal(UpdateAvailability.UpToDate, UpdateChecker.ParseRelease(d.RootElement, new Version(0, 2, 1)).Availability);
        Equal(UpdateAvailability.UpToDate, UpdateChecker.ParseRelease(d.RootElement, new Version(0, 3, 0)).Availability);
    }),
    ("Update: invalid release metadata", () =>
    {
        using var d = JsonDocument.Parse("""{"tag_name":"not-a-version","html_url":"https://example.invalid"}""");
        Throws<InvalidOperationException>(() => UpdateChecker.ParseRelease(d.RootElement, new Version(0, 2, 0)));
    }),
    ("Update: choose exact distribution and digest", () =>
    {
        var p = Select(Release(), true);
        var i = Select(Release(), false);
        Equal("MediaM8-0.2.1-win-x64-portable.zip", p.Name);
        Equal("MediaM8-0.2.1-win-x64-setup.exe", i.Name);
        Equal(new string('a', 64), p.Sha256);
        Equal(new string('b', 64), i.Sha256);
    }),
    ("Update: reject missing asset or digest", () =>
    {
        Throws<InvalidOperationException>(() => Select(Release(portable: false), true));
        Throws<InvalidOperationException>(() => Select(Release(validDigest: false), true));
    }),
    ("Update: reject draft, prerelease, wrong version and foreign URL", () =>
    {
        Throws<InvalidOperationException>(() => Select(Release(draft: true), true));
        Throws<InvalidOperationException>(() => Select(Release(prerelease: true), true));
        Throws<InvalidOperationException>(() => Select(Release(tag: "v0.2.2"), true));
        Throws<InvalidOperationException>(() => Select(Release(foreignUrl: true), true));
    })
};

var failures = 0;
foreach (var (name, run) in tests)
{
    try
    {
        run();
        Console.WriteLine($"PASS  {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"FAIL  {name}: {ex.Message}");
    }
}

Console.WriteLine($"{tests.Length - failures}/{tests.Length} regression tests passed.");
return failures == 0 ? 0 : 1;

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"Expected {expected}, got {actual}.");
}

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}

static DownloadProgressSnapshot Progress(string kind, string json)
{
    if (!DownloadProgressParser.TryParse(DownloadProgressParser.Marker + kind + "|" + json, out var p) || p is null)
        throw new Exception("Progress was not parsed.");
    return p;
}

static ProcessStartInfo PlaylistFixture(params string[] arguments)
{
    var executable = Environment.ProcessPath ?? throw new Exception("Missing fixture executable.");
    var start = new ProcessStartInfo(executable)
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
    foreach (var argument in arguments)
        start.ArgumentList.Add(argument);
    return start;
}

static DownloadRequest Request(string id) =>
    new("https://example.invalid/" + id, id, "fixture-folder", false, true, false, true, "Best (VBR 0)", "Best");

static void WithSettings(Action<SettingsStore, string> test)
{
    var directory = Path.Combine(Path.GetTempPath(), "MediaM8-regression-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
        var path = Path.Combine(directory, "settings.json");
        test(new SettingsStore(path), path);
    }
    finally { Directory.Delete(directory, recursive: true); }
}

static UpdateAsset Select(string json, bool portable)
{
    using var d = JsonDocument.Parse(json);
    return UpdateAssetSelector.Select(d.RootElement, new Version(0, 2, 1), new Version(0, 2, 0), portable);
}

static string Release(bool portable = true, bool validDigest = true, bool draft = false,
    bool prerelease = false, string tag = "v0.2.1", bool foreignUrl = false)
{
    const string baseUrl = "https://github.com/Zeekroket/Media-M8/releases/download/v0.2.1/";
    const string portableName = "MediaM8-0.2.1-win-x64-portable.zip";
    const string setupName = "MediaM8-0.2.1-win-x64-setup.exe";
    var assets = new List<object>
    {
        new { name = setupName, digest = "sha256:" + new string('b', 64), browser_download_url = baseUrl + setupName }
    };
    if (portable)
        assets.Add(new { name = portableName, digest = validDigest ? "sha256:" + new string('a', 64) : null,
            browser_download_url = (foreignUrl ? "https://example.invalid/" : baseUrl) + portableName });
    return JsonSerializer.Serialize(new { tag_name = tag, html_url = "https://github.com/Zeekroket/Media-M8/releases/tag/" + tag, draft, prerelease, assets });
}
