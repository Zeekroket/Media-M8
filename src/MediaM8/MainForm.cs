using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MediaM8;

internal sealed class MainForm : Form
{
    private readonly SessionLogger _logger;

    private readonly TextBox _txtUrl = new();
    private readonly TextBox _txtFolder = new();
    private readonly RadioButton _radioMp3 = new();
    private readonly RadioButton _radioVideo = new();
    private readonly ComboBox _cmbVideoQuality = new();
    private readonly ComboBox _cmbAudioQuality = new();
    private readonly CheckBox _chkPlaylist = new();
    private readonly CheckBox _chkArchive = new();
    private readonly CheckBox _chkOpenFolder = new();
    private readonly Button _btnBrowse = new();
    private readonly Button _btnDownload = new();
    private readonly TextBox _txtLog = new();
    private readonly Label _status = new();

    private Process? _downloadProcess;
    private bool _isBusy;

    public MainForm(SessionLogger logger)
    {
        _logger = logger;

        Text = $"{AppInfo.Name} {AppInfo.Version}";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.None;
        Size = new Size(780, 690);
        MinimumSize = new Size(780, 690);
        Font = new Font("Segoe UI", 9f);

        try
        {
            var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icon is not null)
            {
                Icon = icon;
            }
        }
        catch
        {
            // The executable already carries the application icon.
        }

        BuildUi();
        FormClosing += MainForm_FormClosing;
    }

    private void BuildUi()
    {
        Controls.Add(NewLabel("Media URL", 20, 20));

        _txtUrl.Location = new Point(20, 43);
        _txtUrl.Size = new Size(720, 25);
        Controls.Add(_txtUrl);

        var groupType = NewGroupBox("Download as", 20, 85, 300, 75);
        Controls.Add(groupType);

        _radioMp3.Text = "MP3";
        _radioMp3.Location = new Point(20, 30);
        _radioMp3.AutoSize = true;
        _radioMp3.Checked = true;
        groupType.Controls.Add(_radioMp3);

        _radioVideo.Text = "Video (MKV)";
        _radioVideo.Location = new Point(110, 30);
        _radioVideo.AutoSize = true;
        groupType.Controls.Add(_radioVideo);

        var groupVideoQuality = NewGroupBox("Video quality", 340, 85, 190, 75);
        Controls.Add(groupVideoQuality);

        _cmbVideoQuality.Location = new Point(20, 29);
        _cmbVideoQuality.Size = new Size(145, 25);
        _cmbVideoQuality.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbVideoQuality.Items.AddRange(new object[] { "480p", "720p", "1080p", "Best" });
        _cmbVideoQuality.SelectedIndex = 0;
        _cmbVideoQuality.Enabled = false;
        groupVideoQuality.Controls.Add(_cmbVideoQuality);

        var groupAudioQuality = NewGroupBox("Audio quality (MP3)", 550, 85, 190, 75);
        Controls.Add(groupAudioQuality);

        _cmbAudioQuality.Location = new Point(20, 29);
        _cmbAudioQuality.Size = new Size(145, 25);
        _cmbAudioQuality.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbAudioQuality.Items.AddRange(
            new object[] { "Best (VBR 0)", "320 kbps", "256 kbps", "192 kbps", "128 kbps" });
        _cmbAudioQuality.SelectedIndex = 0;
        groupAudioQuality.Controls.Add(_cmbAudioQuality);

        _radioMp3.CheckedChanged += (_, _) => UpdateQualityControls();
        _radioVideo.CheckedChanged += (_, _) => UpdateQualityControls();

        Controls.Add(NewLabel("Save to", 20, 180));

        _txtFolder.Location = new Point(20, 203);
        _txtFolder.Size = new Size(625, 25);
        _txtFolder.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            AppInfo.Name);
        Controls.Add(_txtFolder);

        _btnBrowse.Text = "Browse...";
        _btnBrowse.Location = new Point(655, 201);
        _btnBrowse.Size = new Size(85, 29);
        _btnBrowse.Click += BrowseButton_Click;
        Controls.Add(_btnBrowse);

        _chkPlaylist.Text = "Download playlist";
        _chkPlaylist.Location = new Point(20, 250);
        _chkPlaylist.AutoSize = true;
        _chkPlaylist.Checked = true;
        Controls.Add(_chkPlaylist);

        _chkArchive.Text = "Skip previously downloaded items (downloaded.txt)";
        _chkArchive.Location = new Point(200, 250);
        _chkArchive.AutoSize = true;
        _chkArchive.Checked = true;
        Controls.Add(_chkArchive);

        _chkOpenFolder.Text = "Open folder when finished";
        _chkOpenFolder.Location = new Point(560, 250);
        _chkOpenFolder.AutoSize = true;
        Controls.Add(_chkOpenFolder);

        _btnDownload.Text = "DOWNLOAD";
        _btnDownload.Location = new Point(20, 290);
        _btnDownload.Size = new Size(720, 42);
        _btnDownload.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        _btnDownload.Click += DownloadButton_Click;
        Controls.Add(_btnDownload);

        Controls.Add(NewLabel("Log", 20, 350));

        _txtLog.Location = new Point(20, 373);
        _txtLog.Size = new Size(720, 220);
        _txtLog.Multiline = true;
        _txtLog.ScrollBars = ScrollBars.Vertical;
        _txtLog.ReadOnly = true;
        _txtLog.WordWrap = false;
        _txtLog.Font = new Font("Consolas", 9f);
        Controls.Add(_txtLog);

        _status.Text = "Ready.";
        _status.Location = new Point(20, 610);
        _status.AutoSize = true;
        Controls.Add(_status);
    }

    private static Label NewLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true
        };
    }

    private static GroupBox NewGroupBox(
        string text,
        int x,
        int y,
        int width,
        int height)
    {
        return new GroupBox
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height)
        };
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose the folder where Media M8 should save downloads."
        };

        if (Directory.Exists(_txtFolder.Text))
        {
            dialog.SelectedPath = _txtFolder.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _txtFolder.Text = dialog.SelectedPath;
        }
    }

    private async void DownloadButton_Click(object? sender, EventArgs e)
    {
        if (_isBusy)
        {
            return;
        }

        var url = _txtUrl.Text.Trim();
        var folder = _txtFolder.Text.Trim();

        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show(
                this,
                "Enter a media URL first.",
                AppInfo.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUri) ||
            (parsedUri.Scheme != Uri.UriSchemeHttp &&
             parsedUri.Scheme != Uri.UriSchemeHttps))
        {
            MessageBox.Show(
                this,
                "Enter a valid HTTP or HTTPS URL.",
                AppInfo.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(folder))
        {
            MessageBox.Show(
                this,
                "Choose an output folder first.",
                AppInfo.Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var toolsDirectory = Path.Combine(AppContext.BaseDirectory, "tools");
        var runtimeTools = RuntimeToolManager.GetToolPaths(toolsDirectory);
        var missingTools = RuntimeToolManager.GetMissingTools(runtimeTools);

        _logger.Write(
            "Runtime detection: " +
            $"yt-dlp={runtimeTools.YtDlp ?? string.Empty}; " +
            $"ffmpeg={runtimeTools.FFmpeg ?? string.Empty}; " +
            $"ffprobe={runtimeTools.FFprobe ?? string.Empty}; " +
            $"deno={runtimeTools.Deno ?? string.Empty}");

        _logger.Write(
            "Missing runtime tools: " +
            (missingTools.Count == 0 ? "none" : string.Join(", ", missingTools)));

        if (missingTools.Count > 0)
        {
            if (!ShowInstallPrompt(missingTools))
            {
                _status.Text = "Required tools are missing.";
                return;
            }

            AddUiLog("Installing required tools from official project releases...");
            _status.Text = "Installing required tools...";
            SetUiBusy(true);

            try
            {
                var progress = new Progress<string>(AddUiLog);

                await Task.Run(
                    () => RuntimeToolManager.InstallMissingToolsAsync(
                        toolsDirectory,
                        _logger,
                        progress,
                        CancellationToken.None));

                runtimeTools = RuntimeToolManager.GetToolPaths(toolsDirectory);
                missingTools = RuntimeToolManager.GetMissingTools(runtimeTools);

                if (missingTools.Count > 0)
                {
                    throw new InvalidOperationException(
                        "One or more required tools are still unavailable: " +
                        string.Join(", ", missingTools));
                }

                _status.Text = "Required tools installed.";
            }
            catch (Exception ex)
            {
                AddUiLog($"Tool installation failed: {ex.Message}");
                _logger.Write($"Tool installation failed: {ex}", "ERROR");
                _status.Text = "Tool installation failed.";

                MessageBox.Show(
                    this,
                    $"Media M8 could not install the required tools." +
                    $"{Environment.NewLine}{Environment.NewLine}{ex.Message}" +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"Session log: {_logger.LogFilePath}",
                    "Tool installation failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            finally
            {
                SetUiBusy(false);
            }
        }

        if (runtimeTools.YtDlp is null ||
            runtimeTools.FFmpeg is null ||
            runtimeTools.FFprobe is null ||
            runtimeTools.Deno is null)
        {
            _status.Text = "Required tools are missing.";
            return;
        }

        try
        {
            Directory.CreateDirectory(folder);
        }
        catch
        {
            MessageBox.Show(
                this,
                $"The output folder could not be created: {folder}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var arguments = BuildYtDlpArguments(url, folder, runtimeTools);

        _txtLog.Clear();
        AddUiLog($"Output folder: {folder}");
        AddUiLog(
            "Mode: " +
            (_radioMp3.Checked
                ? $"MP3 - {_cmbAudioQuality.SelectedItem}"
                : $"Video - {_cmbVideoQuality.SelectedItem}"));
        AddUiLog($"Playlist: {(_chkPlaylist.Checked ? "yes" : "no")}");
        AddUiLog($"Open folder when finished: {(_chkOpenFolder.Checked ? "yes" : "no")}");
        AddUiLog(string.Empty);

        SetUiBusy(true);
        _status.Text = "Downloading...";

        try
        {
            var exitCode = await RunDownloadAsync(runtimeTools.YtDlp, arguments);

            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    $"yt-dlp exited with code {exitCode}.");
            }

            AddUiLog(string.Empty);
            AddUiLog("Done.");
            _status.Text = "Download completed.";

            if (_chkOpenFolder.Checked && Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            AddUiLog(string.Empty);
            AddUiLog($"Download failed: {ex.Message}");
            _logger.Write($"Download failed: {ex}", "ERROR");
            _status.Text = "Download failed.";
        }
        finally
        {
            _downloadProcess?.Dispose();
            _downloadProcess = null;
            SetUiBusy(false);
        }
    }

    private List<string> BuildYtDlpArguments(
        string url,
        string folder,
        RuntimeToolPaths runtimeTools)
    {
        var arguments = new List<string>();

        if (IsYouTubeUrl(url))
        {
            arguments.Add("--extractor-args");
            arguments.Add("youtube:player_client=default,-web_embedded");
        }

        arguments.Add("--js-runtimes");
        arguments.Add("deno:" + runtimeTools.Deno);

        arguments.Add("--ffmpeg-location");
        arguments.Add(Path.GetDirectoryName(runtimeTools.FFmpeg)!);

        string outputTemplate;

        if (_chkPlaylist.Checked)
        {
            arguments.Add("--yes-playlist");
            outputTemplate = Path.Combine(
                folder,
                "%(playlist_index)03d - %(title)s.%(ext)s");
        }
        else
        {
            arguments.Add("--no-playlist");
            outputTemplate = Path.Combine(folder, "%(title)s.%(ext)s");
        }

        if (_chkArchive.Checked)
        {
            arguments.Add("--download-archive");
            arguments.Add(Path.Combine(folder, "downloaded.txt"));
        }

        if (_radioMp3.Checked)
        {
            arguments.Add("-f");
            arguments.Add("ba/b");
            arguments.Add("-x");
            arguments.Add("--audio-format");
            arguments.Add("mp3");
            arguments.Add("--audio-quality");
            arguments.Add(GetAudioQualityArgument());
        }
        else
        {
            arguments.Add("-f");
            arguments.Add(GetVideoFormatArgument());
            arguments.Add("--merge-output-format");
            arguments.Add("mkv");
        }

        arguments.Add("-o");
        arguments.Add(outputTemplate);
        arguments.Add(url);

        return arguments;
    }

    private string GetAudioQualityArgument()
    {
        return _cmbAudioQuality.SelectedItem?.ToString() switch
        {
            "320 kbps" => "320K",
            "256 kbps" => "256K",
            "192 kbps" => "192K",
            "128 kbps" => "128K",
            _ => "0"
        };
    }

    private string GetVideoFormatArgument()
    {
        return _cmbVideoQuality.SelectedItem?.ToString() switch
        {
            "480p" => "bv*[height<=480]+ba/b[height<=480]",
            "720p" => "bv*[height<=720]+ba/b[height<=720]",
            "1080p" => "bv*[height<=1080]+ba/b[height<=1080]",
            _ => "bv*+ba/b"
        };
    }

    private async Task<int> RunDownloadAsync(
        string ytDlpPath,
        IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                AddUiLog(args.Data);
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                AddUiLog(args.Data);
            }
        };

        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Could not start yt-dlp.");
        }

        _downloadProcess = process;
        _logger.Write($"yt-dlp process started with id {process.Id}.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();
        process.WaitForExit();

        _logger.Write($"yt-dlp process exited with code {process.ExitCode}.");
        return process.ExitCode;
    }

    private bool ShowInstallPrompt(IReadOnlyList<string> missingTools)
    {
        using var dialog = new Form
        {
            Text = "Required tools are missing",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(465, 190),
            Font = new Font("Segoe UI", 9f)
        };

        var label = new Label
        {
            Location = new Point(20, 18),
            Size = new Size(425, 105),
            Text =
                "Media M8 needs the following tools:" +
                Environment.NewLine +
                Environment.NewLine +
                string.Join(", ", missingTools) +
                Environment.NewLine +
                Environment.NewLine +
                "Install them now from their official project releases?"
        };
        dialog.Controls.Add(label);

        var installButton = new Button
        {
            Text = "Install",
            Location = new Point(275, 142),
            Size = new Size(80, 30),
            DialogResult = DialogResult.OK
        };
        dialog.Controls.Add(installButton);

        var cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(365, 142),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel
        };
        dialog.Controls.Add(cancelButton);

        dialog.AcceptButton = installButton;
        dialog.CancelButton = cancelButton;

        return dialog.ShowDialog(this) == DialogResult.OK;
    }

    private static bool IsYouTubeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        return host == "youtu.be" ||
               host == "youtube.com" ||
               host.EndsWith(".youtube.com", StringComparison.Ordinal);
    }

    private void AddUiLog(string text)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AddUiLog(text)));
            return;
        }

        _txtLog.AppendText(text + Environment.NewLine);
        _txtLog.SelectionStart = _txtLog.TextLength;
        _txtLog.ScrollToCaret();

        try
        {
            _logger.Write(text, "UI");
        }
        catch
        {
            // Logging must never interrupt the application.
        }
    }

    private void SetUiBusy(bool busy)
    {
        _isBusy = busy;

        _btnDownload.Enabled = !busy;
        _btnBrowse.Enabled = !busy;
        _txtUrl.Enabled = !busy;
        _txtFolder.Enabled = !busy;
        _radioMp3.Enabled = !busy;
        _radioVideo.Enabled = !busy;
        _chkPlaylist.Enabled = !busy;
        _chkArchive.Enabled = !busy;
        _chkOpenFolder.Enabled = !busy;

        UpdateQualityControls();
    }

    private void UpdateQualityControls()
    {
        _cmbVideoQuality.Enabled = !_isBusy && _radioVideo.Checked;
        _cmbAudioQuality.Enabled = !_isBusy && _radioMp3.Checked;
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_downloadProcess is null)
        {
            return;
        }

        try
        {
            if (!_downloadProcess.HasExited)
            {
                _downloadProcess.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _logger.Write(
                $"Could not stop yt-dlp while closing: {ex.Message}",
                "ERROR");
        }
    }
}
