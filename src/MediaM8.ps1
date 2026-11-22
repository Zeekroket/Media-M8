Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

if (-not ('MediaM8.ShellIntegration' -as [type])) {
    Add-Type -TypeDefinition @'
using System.Runtime.InteropServices;

namespace MediaM8
{
    public static class ShellIntegration
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
    }
}
'@
}
[void][MediaM8.ShellIntegration]::SetCurrentProcessExplicitAppUserModelID('Zeekroket.MediaM8')

. (Join-Path $PSScriptRoot 'RuntimeTools.ps1')

$script:AppName = 'Media M8'
$script:AppVersion = '0.1.0'
$script:downloadJob = $null
$script:runtimeInstallProcess = $null
$script:installerLogReadOffset = 0
$script:lastInstallerHeartbeat = [DateTime]::MinValue
$script:lastDownloadFolder = $null
$script:openFolderWhenDone = $false

$script:LogDirectory = Join-Path $PSScriptRoot 'logs'
New-Item -ItemType Directory -Path $script:LogDirectory -Force | Out-Null
$script:LogFile = Join-Path $script:LogDirectory ('MediaM8-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')

function Write-DiagnosticLog {
    param([Parameter(Mandatory = $true)][string]$Message)

    $line = ('[{0}] {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'), $Message)
    Add-Content -LiteralPath $script:LogFile -Value $line -Encoding UTF8
}

Write-DiagnosticLog ('Media M8 ' + $script:AppVersion + ' session started.')
Write-DiagnosticLog ('Script path: ' + $PSScriptRoot)

function Find-Executable {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [string[]]$SearchDirectories = @(),
        [switch]$AllowPathLookup
    )

    foreach ($directory in $SearchDirectories) {
        if ([string]::IsNullOrWhiteSpace($directory)) { continue }
        $candidate = Join-Path $directory $Name
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    if ($AllowPathLookup) {
        $command = Get-Command $Name -ErrorAction SilentlyContinue
        if ($command) { return $command.Source }
    }

    return $null
}

function Test-IsYouTubeUrl {
    param([Parameter(Mandatory = $true)][string]$Url)

    try {
        $uri = [Uri]$Url
        $hostName = $uri.Host.ToLowerInvariant()
        return ($hostName -eq 'youtu.be' -or $hostName -eq 'youtube.com' -or $hostName.EndsWith('.youtube.com'))
    }
    catch {
        return $false
    }
}

function New-Label {
    param([string]$Text, [int]$X, [int]$Y)
    $control = New-Object System.Windows.Forms.Label
    $control.Text = $Text
    $control.Location = New-Object System.Drawing.Point($X, $Y)
    $control.AutoSize = $true
    return $control
}

function New-GroupBox {
    param([string]$Text, [int]$X, [int]$Y, [int]$Width, [int]$Height)
    $control = New-Object System.Windows.Forms.GroupBox
    $control.Text = $Text
    $control.Location = New-Object System.Drawing.Point($X, $Y)
    $control.Size = New-Object System.Drawing.Size($Width, $Height)
    return $control
}

$form = New-Object System.Windows.Forms.Form
$form.Text = "$($script:AppName) $($script:AppVersion)"
$form.StartPosition = 'CenterScreen'
$form.Size = New-Object System.Drawing.Size(780, 690)
$form.MinimumSize = New-Object System.Drawing.Size(780, 690)
$form.Font = New-Object System.Drawing.Font('Segoe UI', 9)
$iconPath = Join-Path $PSScriptRoot 'MediaM8.ico'
if (-not (Test-Path -LiteralPath $iconPath)) {
    $iconPath = Join-Path $PSScriptRoot '..\assets\MediaM8.ico'
}
if (Test-Path -LiteralPath $iconPath) {
    $form.Icon = New-Object System.Drawing.Icon($iconPath)
}

$form.Controls.Add((New-Label -Text 'Media URL' -X 20 -Y 20))

$txtUrl = New-Object System.Windows.Forms.TextBox
$txtUrl.Location = New-Object System.Drawing.Point(20, 43)
$txtUrl.Size = New-Object System.Drawing.Size(720, 25)
$form.Controls.Add($txtUrl)

$groupType = New-GroupBox -Text 'Download as' -X 20 -Y 85 -Width 300 -Height 75
$form.Controls.Add($groupType)

$radioMp3 = New-Object System.Windows.Forms.RadioButton
$radioMp3.Text = 'MP3'
$radioMp3.Location = New-Object System.Drawing.Point(20, 30)
$radioMp3.AutoSize = $true
$radioMp3.Checked = $true
$groupType.Controls.Add($radioMp3)

$radioVideo = New-Object System.Windows.Forms.RadioButton
$radioVideo.Text = 'Video (MKV)'
$radioVideo.Location = New-Object System.Drawing.Point(110, 30)
$radioVideo.AutoSize = $true
$groupType.Controls.Add($radioVideo)

$groupVideoQuality = New-GroupBox -Text 'Video quality' -X 340 -Y 85 -Width 190 -Height 75
$form.Controls.Add($groupVideoQuality)

$cmbVideoQuality = New-Object System.Windows.Forms.ComboBox
$cmbVideoQuality.Location = New-Object System.Drawing.Point(20, 29)
$cmbVideoQuality.Size = New-Object System.Drawing.Size(145, 25)
$cmbVideoQuality.DropDownStyle = 'DropDownList'
@('480p', '720p', '1080p', 'Best') | ForEach-Object { [void]$cmbVideoQuality.Items.Add($_) }
$cmbVideoQuality.SelectedIndex = 0
$cmbVideoQuality.Enabled = $false
$groupVideoQuality.Controls.Add($cmbVideoQuality)

$groupAudioQuality = New-GroupBox -Text 'Audio quality (MP3)' -X 550 -Y 85 -Width 190 -Height 75
$form.Controls.Add($groupAudioQuality)

$cmbAudioQuality = New-Object System.Windows.Forms.ComboBox
$cmbAudioQuality.Location = New-Object System.Drawing.Point(20, 29)
$cmbAudioQuality.Size = New-Object System.Drawing.Size(145, 25)
$cmbAudioQuality.DropDownStyle = 'DropDownList'
@('Best (VBR 0)', '320 kbps', '256 kbps', '192 kbps', '128 kbps') | ForEach-Object { [void]$cmbAudioQuality.Items.Add($_) }
$cmbAudioQuality.SelectedIndex = 0
$groupAudioQuality.Controls.Add($cmbAudioQuality)

$updateQualityControls = {
    $cmbVideoQuality.Enabled = $radioVideo.Checked
    $cmbAudioQuality.Enabled = $radioMp3.Checked
}
$radioMp3.Add_CheckedChanged($updateQualityControls)
$radioVideo.Add_CheckedChanged($updateQualityControls)

$form.Controls.Add((New-Label -Text 'Save to' -X 20 -Y 180))

$txtFolder = New-Object System.Windows.Forms.TextBox
$txtFolder.Location = New-Object System.Drawing.Point(20, 203)
$txtFolder.Size = New-Object System.Drawing.Size(625, 25)
$txtFolder.Text = Join-Path (Join-Path ([Environment]::GetFolderPath('UserProfile')) 'Downloads') $script:AppName
$form.Controls.Add($txtFolder)

$btnBrowse = New-Object System.Windows.Forms.Button
$btnBrowse.Text = 'Browse...'
$btnBrowse.Location = New-Object System.Drawing.Point(655, 201)
$btnBrowse.Size = New-Object System.Drawing.Size(85, 29)
$form.Controls.Add($btnBrowse)

$btnBrowse.Add_Click({
    $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
    $dialog.Description = 'Choose the folder where Media M8 should save downloads.'
    if (Test-Path -LiteralPath $txtFolder.Text) { $dialog.SelectedPath = $txtFolder.Text }
    if ($dialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        $txtFolder.Text = $dialog.SelectedPath
    }
})

$chkPlaylist = New-Object System.Windows.Forms.CheckBox
$chkPlaylist.Text = 'Download playlist'
$chkPlaylist.Location = New-Object System.Drawing.Point(20, 250)
$chkPlaylist.AutoSize = $true
$chkPlaylist.Checked = $true
$form.Controls.Add($chkPlaylist)

$chkArchive = New-Object System.Windows.Forms.CheckBox
$chkArchive.Text = 'Skip previously downloaded items (downloaded.txt)'
$chkArchive.Location = New-Object System.Drawing.Point(200, 250)
$chkArchive.AutoSize = $true
$chkArchive.Checked = $true
$form.Controls.Add($chkArchive)

$chkOpenFolder = New-Object System.Windows.Forms.CheckBox
$chkOpenFolder.Text = 'Open folder when finished'
$chkOpenFolder.Location = New-Object System.Drawing.Point(560, 250)
$chkOpenFolder.AutoSize = $true
$form.Controls.Add($chkOpenFolder)

$btnDownload = New-Object System.Windows.Forms.Button
$btnDownload.Text = 'DOWNLOAD'
$btnDownload.Location = New-Object System.Drawing.Point(20, 290)
$btnDownload.Size = New-Object System.Drawing.Size(720, 42)
$btnDownload.Font = New-Object System.Drawing.Font('Segoe UI', 10, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($btnDownload)

$form.Controls.Add((New-Label -Text 'Log' -X 20 -Y 350))

$txtLog = New-Object System.Windows.Forms.TextBox
$txtLog.Location = New-Object System.Drawing.Point(20, 373)
$txtLog.Size = New-Object System.Drawing.Size(720, 220)
$txtLog.Multiline = $true
$txtLog.ScrollBars = 'Vertical'
$txtLog.ReadOnly = $true
$txtLog.WordWrap = $false
$txtLog.Font = New-Object System.Drawing.Font('Consolas', 9)
$form.Controls.Add($txtLog)

$status = New-Label -Text 'Ready.' -X 20 -Y 610
$form.Controls.Add($status)

function Add-LogLine {
    param([string]$Text)

    $txtLog.AppendText($Text + [Environment]::NewLine)
    $txtLog.SelectionStart = $txtLog.TextLength
    $txtLog.ScrollToCaret()

    try {
        Write-DiagnosticLog ('UI: ' + $Text)
    }
    catch {
        # Logging must never interrupt the application.
    }
}

function Set-UiBusy {
    param([bool]$Busy)

    $btnDownload.Enabled = -not $Busy
    $btnBrowse.Enabled = -not $Busy
    $txtUrl.Enabled = -not $Busy
    $txtFolder.Enabled = -not $Busy
    $radioMp3.Enabled = -not $Busy
    $radioVideo.Enabled = -not $Busy
    $cmbVideoQuality.Enabled = (-not $Busy) -and $radioVideo.Checked
    $cmbAudioQuality.Enabled = (-not $Busy) -and $radioMp3.Checked
    $chkPlaylist.Enabled = -not $Busy
    $chkArchive.Enabled = -not $Busy
    $chkOpenFolder.Enabled = -not $Busy
}

$installTimer = New-Object System.Windows.Forms.Timer
$installTimer.Interval = 250
$installTimer.Add_Tick({
    if (-not $script:runtimeInstallProcess) { return }

    if (([DateTime]::UtcNow - $script:lastInstallerHeartbeat).TotalSeconds -ge 5) {
        Write-DiagnosticLog ('UI heartbeat: installer process running=' + (-not $script:runtimeInstallProcess.HasExited))
        $script:lastInstallerHeartbeat = [DateTime]::UtcNow
    }

    try {
        $allLines = @(Get-Content -LiteralPath $script:LogFile -ErrorAction SilentlyContinue)
        if ($allLines.Count -gt $script:installerLogReadOffset) {
            $newLines = $allLines[$script:installerLogReadOffset..($allLines.Count - 1)]
            foreach ($line in $newLines) {
                if ($line -match 'INSTALLER:\s+(.*)$') {
                    Add-LogLine $matches[1]
                }
            }
            $script:installerLogReadOffset = $allLines.Count
        }
    }
    catch {
        # Diagnostic log reading must never interrupt the UI.
    }

    if ($script:runtimeInstallProcess.HasExited) {
        $installTimer.Stop()
        $exitCode = $script:runtimeInstallProcess.ExitCode
        Write-DiagnosticLog ('Runtime installer process exited with code: ' + $exitCode)

        $script:runtimeInstallProcess.Dispose()
        $script:runtimeInstallProcess = $null
        Set-UiBusy $false

        if ($exitCode -ne 0) {
            $status.Text = 'Tool installation failed.'
            [System.Windows.Forms.MessageBox]::Show(
                "Media M8 could not install the required tools.$([Environment]::NewLine)$([Environment]::NewLine)See the session log for details:$([Environment]::NewLine)$script:LogFile",
                'Tool installation failed',
                'OK',
                'Error'
            ) | Out-Null
            return
        }

        $toolsDirectory = Join-Path $PSScriptRoot 'tools'
        $paths = Get-MediaM8RuntimeToolPaths -SearchDirectories @($toolsDirectory)
        $missing = @(Get-MediaM8MissingRuntimeTools -ToolPaths $paths)

        if ($missing.Count -gt 0) {
            $message = 'One or more required tools are still unavailable: ' + ($missing -join ', ')
            Add-LogLine $message
            $status.Text = 'Required tools are missing.'
            [System.Windows.Forms.MessageBox]::Show($message, 'Required tools missing', 'OK', 'Error') | Out-Null
            return
        }

        $status.Text = 'Required tools installed.'
        Add-LogLine 'Required tools are ready.'
        $btnDownload.PerformClick()
    }
})
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 500
$timer.Add_Tick({
    if (-not $script:downloadJob) { return }

    Receive-Job -Job $script:downloadJob -ErrorAction SilentlyContinue | ForEach-Object { Add-LogLine ([string]$_) }

    if ($script:downloadJob.State -in @('Completed', 'Failed', 'Stopped')) {
        $timer.Stop()
        Receive-Job -Job $script:downloadJob -ErrorAction SilentlyContinue | ForEach-Object { Add-LogLine ([string]$_) }

        if ($script:downloadJob.State -eq 'Completed') {
            Add-LogLine ''
            Add-LogLine 'Done.'
            $status.Text = 'Download completed.'
            if ($script:openFolderWhenDone -and $script:lastDownloadFolder -and (Test-Path -LiteralPath $script:lastDownloadFolder)) {
                Start-Process -FilePath 'explorer.exe' -ArgumentList $script:lastDownloadFolder
            }
        }
        elseif ($script:downloadJob.State -eq 'Failed') {
            Add-LogLine ''
            Add-LogLine 'Download failed.'
            $status.Text = 'Download failed.'
        }
        else {
            Add-LogLine ''
            Add-LogLine 'Download stopped.'
            $status.Text = 'Download stopped.'
        }

        Remove-Job -Job $script:downloadJob -Force -ErrorAction SilentlyContinue
        $script:downloadJob = $null
        Set-UiBusy $false
    }
})

$btnDownload.Add_Click({
    $url = $txtUrl.Text.Trim()
    $folder = $txtFolder.Text.Trim()

    if ([string]::IsNullOrWhiteSpace($url)) {
        [System.Windows.Forms.MessageBox]::Show('Enter a media URL first.', $script:AppName, 'OK', 'Warning') | Out-Null
        return
    }

    $parsedUri = $null
    if (-not [Uri]::TryCreate($url, [UriKind]::Absolute, [ref]$parsedUri) -or $parsedUri.Scheme -notin @('http', 'https')) {
        [System.Windows.Forms.MessageBox]::Show('Enter a valid HTTP or HTTPS URL.', $script:AppName, 'OK', 'Warning') | Out-Null
        return
    }

    if ([string]::IsNullOrWhiteSpace($folder)) {
        [System.Windows.Forms.MessageBox]::Show('Choose an output folder first.', $script:AppName, 'OK', 'Warning') | Out-Null
        return
    }

    $toolsDirectory = Join-Path $PSScriptRoot 'tools'
    $portableDirectories = @($toolsDirectory)

    $runtimeTools = Get-MediaM8RuntimeToolPaths -SearchDirectories $portableDirectories
    $missingTools = @(Get-MediaM8MissingRuntimeTools -ToolPaths $runtimeTools)

    Write-DiagnosticLog ('Runtime detection: yt-dlp=' + [string]$runtimeTools.YtDlp + '; ffmpeg=' + [string]$runtimeTools.FFmpeg + '; ffprobe=' + [string]$runtimeTools.FFprobe + '; deno=' + [string]$runtimeTools.Deno)
    Write-DiagnosticLog ('Missing runtime tools: ' + $(if ($missingTools.Count -gt 0) { $missingTools -join ', ' } else { 'none' }))

    if ($missingTools.Count -gt 0) {
        if (-not (Show-MediaM8InstallPrompt -MissingTools $missingTools -Owner $form)) {
            $status.Text = 'Required tools are missing.'
            return
        }

        Add-LogLine 'Installing required tools from official project releases...'
        $status.Text = 'Installing required tools...'
        Set-UiBusy $true

        try {
            Write-DiagnosticLog 'Starting runtime installer process.'
            $script:installerLogReadOffset = @(Get-Content -LiteralPath $script:LogFile -ErrorAction SilentlyContinue).Count
            $script:lastInstallerHeartbeat = [DateTime]::MinValue
            $script:runtimeInstallProcess = Start-MediaM8RuntimeToolInstall -ToolsDirectory $toolsDirectory -LogFile $script:LogFile
            Write-DiagnosticLog ('Runtime installer process id: ' + $script:runtimeInstallProcess.Id)
            $installTimer.Start()
        }
        catch {
            Set-UiBusy $false
            Add-LogLine ('Tool installation failed: ' + $_.Exception.Message)
            $status.Text = 'Tool installation failed.'
            [System.Windows.Forms.MessageBox]::Show(
                "Media M8 could not start the tool installation.$([Environment]::NewLine)$([Environment]::NewLine)$($_.Exception.Message)",
                'Tool installation failed',
                'OK',
                'Error'
            ) | Out-Null
        }

        return
    }

    $ytDlpPath = $runtimeTools.YtDlp
    $ffmpegPath = $runtimeTools.FFmpeg
    $ffprobePath = $runtimeTools.FFprobe
    $denoPath = $runtimeTools.Deno

    if (-not (Test-Path -LiteralPath $folder)) {
        try {
            New-Item -ItemType Directory -Path $folder -Force | Out-Null
        }
        catch {
            [System.Windows.Forms.MessageBox]::Show("The output folder could not be created: $folder", 'Error', 'OK', 'Error') | Out-Null
            return
        }
    }

    $arguments = New-Object System.Collections.Generic.List[string]

    if (Test-IsYouTubeUrl -Url $url) {
        $arguments.Add('--extractor-args')
        $arguments.Add('youtube:player_client=default,-web_embedded')
    }

    if ($denoPath) {
        $arguments.Add('--js-runtimes')
        $arguments.Add('deno:' + $denoPath)
    }

    $arguments.Add('--ffmpeg-location')
    $arguments.Add((Split-Path -Parent $ffmpegPath))

    if ($chkPlaylist.Checked) {
        $arguments.Add('--yes-playlist')
        $outputTemplate = Join-Path $folder '%(playlist_index)03d - %(title)s.%(ext)s'
    }
    else {
        $arguments.Add('--no-playlist')
        $outputTemplate = Join-Path $folder '%(title)s.%(ext)s'
    }

    if ($chkArchive.Checked) {
        $arguments.Add('--download-archive')
        $arguments.Add((Join-Path $folder 'downloaded.txt'))
    }

    if ($radioMp3.Checked) {
        $arguments.Add('-f')
        $arguments.Add('ba/b')
        $arguments.Add('-x')
        $arguments.Add('--audio-format')
        $arguments.Add('mp3')

        switch ($cmbAudioQuality.SelectedItem.ToString()) {
            '320 kbps' { $audioQuality = '320K' }
            '256 kbps' { $audioQuality = '256K' }
            '192 kbps' { $audioQuality = '192K' }
            '128 kbps' { $audioQuality = '128K' }
            default { $audioQuality = '0' }
        }

        $arguments.Add('--audio-quality')
        $arguments.Add($audioQuality)
    }
    else {
        switch ($cmbVideoQuality.SelectedItem.ToString()) {
            '480p' { $format = 'bv*[height<=480]+ba/b[height<=480]' }
            '720p' { $format = 'bv*[height<=720]+ba/b[height<=720]' }
            '1080p' { $format = 'bv*[height<=1080]+ba/b[height<=1080]' }
            default { $format = 'bv*+ba/b' }
        }

        $arguments.Add('-f')
        $arguments.Add($format)
        $arguments.Add('--merge-output-format')
        $arguments.Add('mkv')
    }

    $arguments.Add('-o')
    $arguments.Add($outputTemplate)
    $arguments.Add($url)

    $txtLog.Clear()
    Add-LogLine ('Output folder: ' + $folder)
    Add-LogLine ('Mode: ' + $(if ($radioMp3.Checked) { 'MP3 - ' + $cmbAudioQuality.SelectedItem } else { 'Video - ' + $cmbVideoQuality.SelectedItem }))
    Add-LogLine ('Playlist: ' + $(if ($chkPlaylist.Checked) { 'yes' } else { 'no' }))
    Add-LogLine ('Open folder when finished: ' + $(if ($chkOpenFolder.Checked) { 'yes' } else { 'no' }))
    Add-LogLine ''

    $script:lastDownloadFolder = $folder
    $script:openFolderWhenDone = $chkOpenFolder.Checked
    Set-UiBusy $true
    $status.Text = 'Downloading...'

    $script:downloadJob = Start-Job -ScriptBlock {
        param($ExePath, $ArgArray)
        & $ExePath @ArgArray 2>&1 | ForEach-Object { $_.ToString() }
        if ($LASTEXITCODE -ne 0) {
            throw "yt-dlp exited with code $LASTEXITCODE."
        }
    } -ArgumentList $ytDlpPath, $arguments.ToArray()

    $timer.Start()
})

$form.Add_FormClosing({
    if ($script:runtimeInstallProcess) {
        try {
            if (-not $script:runtimeInstallProcess.HasExited) {
                $script:runtimeInstallProcess.Kill()
            }
        }
        catch {
        }
        finally {
            $script:runtimeInstallProcess.Dispose()
            $script:runtimeInstallProcess = $null
        }
    }

    if ($script:downloadJob) {
        Stop-Job -Job $script:downloadJob -ErrorAction SilentlyContinue
        Remove-Job -Job $script:downloadJob -Force -ErrorAction SilentlyContinue
        $script:downloadJob = $null
    }
})

Write-DiagnosticLog 'Entering WinForms message loop.'
[void]$form.ShowDialog()
Write-DiagnosticLog 'Media M8 session ended.'
