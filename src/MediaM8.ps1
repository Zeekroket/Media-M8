Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

# Give the PowerShell-hosted WinForms window its own Windows taskbar identity.
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

$script:AppName = 'Media M8'
$script:AppVersion = '0.1.0'
$script:downloadJob = $null
$script:lastDownloadFolder = $null
$script:openFolderWhenDone = $false

function Find-Executable {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [string[]]$SearchDirectories = @()
    )

    foreach ($directory in $SearchDirectories) {
        if ([string]::IsNullOrWhiteSpace($directory)) { continue }
        $candidate = Join-Path $directory $Name
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    return $null
}

function Get-MissingRuntimeTools {
    param([string[]]$SearchDirectories)

    $tools = @(
        @{ Name = 'yt-dlp.exe'; DisplayName = 'yt-dlp' },
        @{ Name = 'ffmpeg.exe'; DisplayName = 'FFmpeg' },
        @{ Name = 'ffprobe.exe'; DisplayName = 'ffprobe' },
        @{ Name = 'deno.exe'; DisplayName = 'Deno' }
    )

    $missing = @()
    foreach ($tool in $tools) {
        if (-not (Find-Executable -Name $tool.Name -SearchDirectories $SearchDirectories)) {
            $missing += $tool
        }
    }
    return $missing
}

function Show-InstallToolsPrompt {
    param([object[]]$MissingTools)

    $dialog = New-Object System.Windows.Forms.Form
    $dialog.Text = 'Required tools are missing'
    $dialog.StartPosition = 'CenterParent'
    $dialog.FormBorderStyle = 'FixedDialog'
    $dialog.MaximizeBox = $false
    $dialog.MinimizeBox = $false
    $dialog.ShowInTaskbar = $false
    $dialog.ClientSize = New-Object System.Drawing.Size(455, 190)
    $dialog.Font = New-Object System.Drawing.Font('Segoe UI', 9)

    $label = New-Object System.Windows.Forms.Label
    $label.Location = New-Object System.Drawing.Point(20, 18)
    $label.Size = New-Object System.Drawing.Size(415, 105)
    $label.Text = @(
        'Media M8 needs the following tools:',
        '',
        (($MissingTools | ForEach-Object { $_.DisplayName }) -join ', '),
        '',
        'Install them now from their official project releases?'
    ) -join [Environment]::NewLine
    $dialog.Controls.Add($label)

    $installButton = New-Object System.Windows.Forms.Button
    $installButton.Text = 'Install'
    $installButton.Location = New-Object System.Drawing.Point(265, 142)
    $installButton.Size = New-Object System.Drawing.Size(80, 30)
    $installButton.DialogResult = [System.Windows.Forms.DialogResult]::OK
    $dialog.AcceptButton = $installButton
    $dialog.Controls.Add($installButton)

    $cancelButton = New-Object System.Windows.Forms.Button
    $cancelButton.Text = 'Cancel'
    $cancelButton.Location = New-Object System.Drawing.Point(355, 142)
    $cancelButton.Size = New-Object System.Drawing.Size(80, 30)
    $cancelButton.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
    $dialog.CancelButton = $cancelButton
    $dialog.Controls.Add($cancelButton)

    return ($dialog.ShowDialog($form) -eq [System.Windows.Forms.DialogResult]::OK)
}

function Install-MediaM8RuntimeTools {
    param([string]$ToolsDirectory)

    New-Item -ItemType Directory -Path $ToolsDirectory -Force | Out-Null

    $status.Text = 'Installing required tools...'
    Add-LogLine 'Installing required tools from official project releases...'
    Set-UiBusy $true

    $installJob = Start-Job -ScriptBlock {
        param($TargetDirectory)

        $ErrorActionPreference = 'Stop'
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

        function Download-File {
            param([string]$Uri, [string]$Destination)
            Invoke-WebRequest -Uri $Uri -OutFile $Destination -UseBasicParsing
        }

        function Assert-Sha256 {
            param([string]$Path, [string]$ExpectedHash, [string]$DisplayName)
            $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($actual -ne $ExpectedHash.ToLowerInvariant()) {
                throw "$DisplayName integrity check failed."
            }
        }

        function Get-ChecksumFromText {
            param([string]$Text, [string]$FileName)
            foreach ($line in ($Text -split "\r?\n")) {
                if ($line -match '^([a-fA-F0-9]{64})\s+\*?(.+) {
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
    $portableDirectories = @($toolsDirectory, $PSScriptRoot)
    $missingTools = @(Get-MissingRuntimeTools -SearchDirectories $portableDirectories)

    if ($missingTools.Count -gt 0) {
        if (-not (Show-InstallToolsPrompt -MissingTools $missingTools)) {
            $status.Text = 'Required tools are missing.'
            return
        }

        try {
            Install-MediaM8RuntimeTools -ToolsDirectory $toolsDirectory
        }
        catch {
            [System.Windows.Forms.MessageBox]::Show(
                "Media M8 could not install the required tools.$([Environment]::NewLine)$([Environment]::NewLine)$($_.Exception.Message)",
                'Tool installation failed',
                'OK',
                'Error'
            ) | Out-Null
            $status.Text = 'Tool installation failed.'
            return
        }
    }

    $ytDlpPath = Find-Executable -Name 'yt-dlp.exe' -SearchDirectories $portableDirectories
    $ffmpegPath = Find-Executable -Name 'ffmpeg.exe' -SearchDirectories $portableDirectories
    $ffprobePath = Find-Executable -Name 'ffprobe.exe' -SearchDirectories $portableDirectories
    $denoPath = Find-Executable -Name 'deno.exe' -SearchDirectories $portableDirectories

    if (-not $ytDlpPath -or -not $ffmpegPath -or -not $ffprobePath -or -not $denoPath) {
        [System.Windows.Forms.MessageBox]::Show(
            'One or more required tools are still unavailable.',
            'Required tools missing',
            'OK',
            'Error'
        ) | Out-Null
        $status.Text = 'Required tools are missing.'
        return
    }

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
    if ($script:downloadJob) {
        Stop-Job -Job $script:downloadJob -ErrorAction SilentlyContinue
        Remove-Job -Job $script:downloadJob -Force -ErrorAction SilentlyContinue
        $script:downloadJob = $null
    }
})

[void]$form.ShowDialog()
) {
                    if ($matches[2].Trim() -eq $FileName) {
                        return $matches[1]
                    }
                }
            }
            throw "No SHA-256 checksum was found for $FileName."
        }

        $tempDirectory = Join-Path ([IO.Path]::GetTempPath()) ('MediaM8-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $tempDirectory -Force | Out-Null

        try {
            $ytDlpTarget = Join-Path $TargetDirectory 'yt-dlp.exe'
            if (-not (Test-Path -LiteralPath $ytDlpTarget)) {
                Write-Output 'Downloading yt-dlp...'
                $ytDlpTemp = Join-Path $tempDirectory 'yt-dlp.exe'
                $ytDlpSums = (Invoke-WebRequest -Uri 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS' -UseBasicParsing).Content
                $ytDlpExpected = Get-ChecksumFromText -Text $ytDlpSums -FileName 'yt-dlp.exe'
                Download-File -Uri 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe' -Destination $ytDlpTemp
                Assert-Sha256 -Path $ytDlpTemp -ExpectedHash $ytDlpExpected -DisplayName 'yt-dlp'
                Move-Item -LiteralPath $ytDlpTemp -Destination $ytDlpTarget -Force
                Write-Output 'yt-dlp installed and verified.'
            }

            $ffmpegTarget = Join-Path $TargetDirectory 'ffmpeg.exe'
            $ffprobeTarget = Join-Path $TargetDirectory 'ffprobe.exe'
            if (-not (Test-Path -LiteralPath $ffmpegTarget) -or -not (Test-Path -LiteralPath $ffprobeTarget)) {
                Write-Output 'Downloading FFmpeg and ffprobe...'
                $ffmpegArchiveName = 'ffmpeg-master-latest-win64-gpl.zip'
                $ffmpegZip = Join-Path $tempDirectory $ffmpegArchiveName
                $ffmpegSums = (Invoke-WebRequest -Uri 'https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/checksums.sha256' -UseBasicParsing).Content
                $ffmpegExpected = Get-ChecksumFromText -Text $ffmpegSums -FileName $ffmpegArchiveName
                Download-File -Uri ('https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/' + $ffmpegArchiveName) -Destination $ffmpegZip
                Assert-Sha256 -Path $ffmpegZip -ExpectedHash $ffmpegExpected -DisplayName 'FFmpeg archive'

                $ffmpegExtract = Join-Path $tempDirectory 'ffmpeg'
                Expand-Archive -LiteralPath $ffmpegZip -DestinationPath $ffmpegExtract -Force
                $ffmpegSource = Get-ChildItem -LiteralPath $ffmpegExtract -Filter 'ffmpeg.exe' -File -Recurse | Select-Object -First 1
                $ffprobeSource = Get-ChildItem -LiteralPath $ffmpegExtract -Filter 'ffprobe.exe' -File -Recurse | Select-Object -First 1
                if (-not $ffmpegSource -or -not $ffprobeSource) {
                    throw 'FFmpeg archive did not contain ffmpeg.exe and ffprobe.exe.'
                }
                Copy-Item -LiteralPath $ffmpegSource.FullName -Destination $ffmpegTarget -Force
                Copy-Item -LiteralPath $ffprobeSource.FullName -Destination $ffprobeTarget -Force
                Write-Output 'FFmpeg and ffprobe installed and verified.'
            }

            $denoTarget = Join-Path $TargetDirectory 'deno.exe'
            if (-not (Test-Path -LiteralPath $denoTarget)) {
                Write-Output 'Downloading Deno...'
                $headers = @{ 'User-Agent' = 'Media-M8' }
                $release = Invoke-RestMethod -Uri 'https://api.github.com/repos/denoland/deno/releases/latest' -Headers $headers
                $denoAssetName = 'deno-x86_64-pc-windows-msvc.zip'
                $denoAsset = $release.assets | Where-Object { $_.name -eq $denoAssetName } | Select-Object -First 1
                $denoChecksumAsset = $release.assets | Where-Object { $_.name -eq ($denoAssetName + '.sha256sum') } | Select-Object -First 1
                if (-not $denoAsset -or -not $denoChecksumAsset) {
                    throw 'The latest Deno release does not contain the expected Windows x64 assets.'
                }

                $denoZip = Join-Path $tempDirectory $denoAssetName
                Download-File -Uri $denoAsset.browser_download_url -Destination $denoZip
                $denoChecksumText = (Invoke-WebRequest -Uri $denoChecksumAsset.browser_download_url -Headers $headers -UseBasicParsing).Content
                $denoExpected = ([regex]::Match($denoChecksumText, '[a-fA-F0-9]{64}')).Value
                if ([string]::IsNullOrWhiteSpace($denoExpected)) {
                    throw 'No SHA-256 checksum was found for Deno.'
                }
                Assert-Sha256 -Path $denoZip -ExpectedHash $denoExpected -DisplayName 'Deno archive'

                $denoExtract = Join-Path $tempDirectory 'deno'
                Expand-Archive -LiteralPath $denoZip -DestinationPath $denoExtract -Force
                $denoSource = Get-ChildItem -LiteralPath $denoExtract -Filter 'deno.exe' -File -Recurse | Select-Object -First 1
                if (-not $denoSource) { throw 'Deno archive did not contain deno.exe.' }
                Copy-Item -LiteralPath $denoSource.FullName -Destination $denoTarget -Force
                Write-Output 'Deno installed and verified.'
            }

            Write-Output 'Required tools are ready.'
        }
        finally {
            Remove-Item -LiteralPath $tempDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
    } -ArgumentList $ToolsDirectory

    try {
        while ($installJob.State -eq 'Running') {
            Receive-Job -Job $installJob -ErrorAction SilentlyContinue | ForEach-Object { Add-LogLine ([string]$_) }
            [System.Windows.Forms.Application]::DoEvents()
            Start-Sleep -Milliseconds 100
        }

        Receive-Job -Job $installJob -ErrorAction SilentlyContinue | ForEach-Object { Add-LogLine ([string]$_) }

        if ($installJob.State -ne 'Completed') {
            $reason = $installJob.ChildJobs[0].JobStateInfo.Reason
            if ($reason) { throw $reason.Message }
            throw 'Tool installation failed.'
        }
    }
    finally {
        Remove-Job -Job $installJob -Force -ErrorAction SilentlyContinue
        Set-UiBusy $false
    }

    $status.Text = 'Required tools installed.'
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

    $portableDirectories = @((Join-Path $PSScriptRoot 'tools'), $PSScriptRoot)
    $ytDlpPath = Find-Executable -Name 'yt-dlp.exe' -SearchDirectories $portableDirectories
    $ffmpegPath = Find-Executable -Name 'ffmpeg.exe' -SearchDirectories $portableDirectories
    $ffprobePath = Find-Executable -Name 'ffprobe.exe' -SearchDirectories $portableDirectories
    $denoPath = Find-Executable -Name 'deno.exe' -SearchDirectories $portableDirectories

    if (-not $ytDlpPath) {
        [System.Windows.Forms.MessageBox]::Show("yt-dlp was not found. Place yt-dlp.exe in the 'tools' folder next to Media M8.", 'yt-dlp not found', 'OK', 'Error') | Out-Null
        return
    }

    if (-not $ffmpegPath -or -not $ffprobePath) {
        [System.Windows.Forms.MessageBox]::Show("ffmpeg and/or ffprobe were not found. Place both files in the 'tools' folder next to Media M8.", 'FFmpeg not found', 'OK', 'Error') | Out-Null
        return
    }

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
    if ($script:downloadJob) {
        Stop-Job -Job $script:downloadJob -ErrorAction SilentlyContinue
        Remove-Job -Job $script:downloadJob -Force -ErrorAction SilentlyContinue
        $script:downloadJob = $null
    }
})

[void]$form.ShowDialog()
