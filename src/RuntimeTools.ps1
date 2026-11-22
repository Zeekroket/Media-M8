function Get-MediaM8RuntimeToolPaths {
    param([string[]]$SearchDirectories)

    [pscustomobject]@{
        YtDlp  = Find-Executable -Name 'yt-dlp.exe' -SearchDirectories $SearchDirectories
        FFmpeg = Find-Executable -Name 'ffmpeg.exe' -SearchDirectories $SearchDirectories
        FFprobe = Find-Executable -Name 'ffprobe.exe' -SearchDirectories $SearchDirectories
        Deno   = Find-Executable -Name 'deno.exe' -SearchDirectories $SearchDirectories
    }
}

function Get-MediaM8MissingRuntimeTools {
    param([Parameter(Mandatory = $true)]$ToolPaths)

    $missing = @()
    if (-not $ToolPaths.YtDlp)  { $missing += 'yt-dlp' }
    if (-not $ToolPaths.FFmpeg) { $missing += 'FFmpeg' }
    if (-not $ToolPaths.FFprobe) { $missing += 'ffprobe' }
    if (-not $ToolPaths.Deno)   { $missing += 'Deno' }
    return $missing
}

function Show-MediaM8InstallPrompt {
    param(
        [string[]]$MissingTools,
        [System.Windows.Forms.IWin32Window]$Owner
    )

    $dialog = New-Object System.Windows.Forms.Form
    $dialog.Text = 'Required tools are missing'
    $dialog.StartPosition = 'CenterParent'
    $dialog.FormBorderStyle = 'FixedDialog'
    $dialog.MaximizeBox = $false
    $dialog.MinimizeBox = $false
    $dialog.ShowInTaskbar = $false
    $dialog.ClientSize = New-Object System.Drawing.Size(465, 190)
    $dialog.Font = New-Object System.Drawing.Font('Segoe UI', 9)

    $label = New-Object System.Windows.Forms.Label
    $label.Location = New-Object System.Drawing.Point(20, 18)
    $label.Size = New-Object System.Drawing.Size(425, 105)
    $label.Text = @(
        'Media M8 needs the following tools:',
        '',
        ($MissingTools -join ', '),
        '',
        'Install them now from their official project releases?'
    ) -join [Environment]::NewLine
    $dialog.Controls.Add($label)

    $installButton = New-Object System.Windows.Forms.Button
    $installButton.Text = 'Install'
    $installButton.Location = New-Object System.Drawing.Point(275, 142)
    $installButton.Size = New-Object System.Drawing.Size(80, 30)
    $installButton.DialogResult = [System.Windows.Forms.DialogResult]::OK
    $dialog.AcceptButton = $installButton
    $dialog.Controls.Add($installButton)

    $cancelButton = New-Object System.Windows.Forms.Button
    $cancelButton.Text = 'Cancel'
    $cancelButton.Location = New-Object System.Drawing.Point(365, 142)
    $cancelButton.Size = New-Object System.Drawing.Size(80, 30)
    $cancelButton.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
    $dialog.CancelButton = $cancelButton
    $dialog.Controls.Add($cancelButton)

    return ($dialog.ShowDialog($Owner) -eq [System.Windows.Forms.DialogResult]::OK)
}

function Start-MediaM8RuntimeToolInstall {
    param([Parameter(Mandatory = $true)][string]$ToolsDirectory)

    New-Item -ItemType Directory -Path $ToolsDirectory -Force | Out-Null

    return Start-Job -ScriptBlock {
        param($TargetDirectory)

        $ErrorActionPreference = 'Stop'
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

        function Download-File {
            param([string]$Uri, [string]$Destination)
            Invoke-WebRequest -Uri $Uri -OutFile $Destination -UseBasicParsing
        }

        function Get-ChecksumFromFile {
            param([string]$Path, [string]$FileName)

            foreach ($line in [IO.File]::ReadAllLines($Path)) {
                $trimmed = $line.Trim()
                if (-not $trimmed.EndsWith($FileName, [StringComparison]::OrdinalIgnoreCase)) {
                    continue
                }

                $parts = $trimmed -split '\s+'
                if ($parts.Count -gt 0 -and $parts[0] -match '^[a-fA-F0-9]{64}$') {
                    return $parts[0]
                }
            }

            throw "No SHA-256 checksum was found for $FileName."
        }

        function Assert-Sha256 {
            param([string]$Path, [string]$ExpectedHash, [string]$DisplayName)

            $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
            if ($actual -ne $ExpectedHash) {
                throw "$DisplayName integrity check failed."
            }
        }

        $tempDirectory = Join-Path ([IO.Path]::GetTempPath()) ('MediaM8-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $tempDirectory -Force | Out-Null

        try {
            $ytDlpTarget = Join-Path $TargetDirectory 'yt-dlp.exe'
            if (-not (Test-Path -LiteralPath $ytDlpTarget)) {
                Write-Output 'Downloading yt-dlp...'
                $ytDlpTemp = Join-Path $tempDirectory 'yt-dlp.exe'
                $ytDlpSumsFile = Join-Path $tempDirectory 'yt-dlp-SHA2-256SUMS'
                Download-File -Uri 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS' -Destination $ytDlpSumsFile
                $ytDlpExpected = Get-ChecksumFromFile -Path $ytDlpSumsFile -FileName 'yt-dlp.exe'
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
                $ffmpegSumsFile = Join-Path $tempDirectory 'ffmpeg-checksums.sha256'
                Download-File -Uri 'https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/checksums.sha256' -Destination $ffmpegSumsFile
                $ffmpegExpected = Get-ChecksumFromFile -Path $ffmpegSumsFile -FileName $ffmpegArchiveName
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
                $denoArchiveName = 'deno-x86_64-pc-windows-msvc.zip'
                $denoZip = Join-Path $tempDirectory $denoArchiveName
                $denoChecksumUri = 'https://github.com/denoland/deno/releases/latest/download/' + $denoArchiveName + '.sha256sum'
                $denoChecksumFile = Join-Path $tempDirectory ($denoArchiveName + '.sha256sum')
                Download-File -Uri $denoChecksumUri -Destination $denoChecksumFile
                $denoChecksumText = [IO.File]::ReadAllText($denoChecksumFile)
                $denoExpected = ([regex]::Match($denoChecksumText, '[a-fA-F0-9]{64}')).Value
                if ([string]::IsNullOrWhiteSpace($denoExpected)) {
                    throw 'No SHA-256 checksum was found for Deno.'
                }
                Download-File -Uri ('https://github.com/denoland/deno/releases/latest/download/' + $denoArchiveName) -Destination $denoZip
                Assert-Sha256 -Path $denoZip -ExpectedHash $denoExpected -DisplayName 'Deno archive'

                $denoExtract = Join-Path $tempDirectory 'deno'
                Expand-Archive -LiteralPath $denoZip -DestinationPath $denoExtract -Force
                $denoSource = Get-ChildItem -LiteralPath $denoExtract -Filter 'deno.exe' -File -Recurse | Select-Object -First 1
                if (-not $denoSource) {
                    throw 'Deno archive did not contain deno.exe.'
                }
                Copy-Item -LiteralPath $denoSource.FullName -Destination $denoTarget -Force
                Write-Output 'Deno installed and verified.'
            }

            Write-Output 'Required tools are ready.'
        }
        finally {
            Remove-Item -LiteralPath $tempDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
    } -ArgumentList $ToolsDirectory
}
