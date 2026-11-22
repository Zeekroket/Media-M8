param(
    [Parameter(Mandatory = $true)][string]$ToolsDirectory,
    [Parameter(Mandatory = $true)][string]$LogFile
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Write-InstallerLog {
    param([string]$Message)

    $line = ('[{0}] INSTALLER: {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'), $Message)
    Add-Content -LiteralPath $LogFile -Value $line -Encoding UTF8
}

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

New-Item -ItemType Directory -Path $ToolsDirectory -Force | Out-Null
$tempDirectory = Join-Path ([IO.Path]::GetTempPath()) ('MediaM8-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDirectory -Force | Out-Null

try {
    Write-InstallerLog ('Installer process started. Target directory: ' + $ToolsDirectory)

    $ytDlpTarget = Join-Path $ToolsDirectory 'yt-dlp.exe'
    if (-not (Test-Path -LiteralPath $ytDlpTarget)) {
        Write-InstallerLog 'Starting yt-dlp installation.'
        $ytDlpTemp = Join-Path $tempDirectory 'yt-dlp.exe'
        $ytDlpSumsFile = Join-Path $tempDirectory 'yt-dlp-SHA2-256SUMS'

        Write-InstallerLog 'Downloading yt-dlp checksum file.'
        Download-File -Uri 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS' -Destination $ytDlpSumsFile
        Write-InstallerLog 'yt-dlp checksum file downloaded.'

        $ytDlpExpected = Get-ChecksumFromFile -Path $ytDlpSumsFile -FileName 'yt-dlp.exe'

        Write-InstallerLog 'Downloading yt-dlp executable.'
        Download-File -Uri 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe' -Destination $ytDlpTemp
        Write-InstallerLog 'yt-dlp executable downloaded.'

        Assert-Sha256 -Path $ytDlpTemp -ExpectedHash $ytDlpExpected -DisplayName 'yt-dlp'
        Move-Item -LiteralPath $ytDlpTemp -Destination $ytDlpTarget -Force
        Write-InstallerLog 'yt-dlp installed and verified.'
    }

    $ffmpegTarget = Join-Path $ToolsDirectory 'ffmpeg.exe'
    $ffprobeTarget = Join-Path $ToolsDirectory 'ffprobe.exe'
    if (-not (Test-Path -LiteralPath $ffmpegTarget) -or -not (Test-Path -LiteralPath $ffprobeTarget)) {
        Write-InstallerLog 'Starting FFmpeg/ffprobe installation.'
        $ffmpegArchiveName = 'ffmpeg-master-latest-win64-gpl.zip'
        $ffmpegZip = Join-Path $tempDirectory $ffmpegArchiveName
        $ffmpegSumsFile = Join-Path $tempDirectory 'ffmpeg-checksums.sha256'

        Write-InstallerLog 'Downloading FFmpeg checksum file.'
        Download-File -Uri 'https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/checksums.sha256' -Destination $ffmpegSumsFile
        Write-InstallerLog 'FFmpeg checksum file downloaded.'

        $ffmpegExpected = Get-ChecksumFromFile -Path $ffmpegSumsFile -FileName $ffmpegArchiveName

        Write-InstallerLog 'Downloading FFmpeg archive.'
        Download-File -Uri ('https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/' + $ffmpegArchiveName) -Destination $ffmpegZip
        Write-InstallerLog 'FFmpeg archive downloaded.'

        Assert-Sha256 -Path $ffmpegZip -ExpectedHash $ffmpegExpected -DisplayName 'FFmpeg archive'

        $ffmpegExtract = Join-Path $tempDirectory 'ffmpeg'
        Write-InstallerLog 'Extracting FFmpeg archive.'
        Expand-Archive -LiteralPath $ffmpegZip -DestinationPath $ffmpegExtract -Force
        Write-InstallerLog 'FFmpeg archive extracted.'

        $ffmpegSource = Get-ChildItem -LiteralPath $ffmpegExtract -Filter 'ffmpeg.exe' -File -Recurse | Select-Object -First 1
        $ffprobeSource = Get-ChildItem -LiteralPath $ffmpegExtract -Filter 'ffprobe.exe' -File -Recurse | Select-Object -First 1
        if (-not $ffmpegSource -or -not $ffprobeSource) {
            throw 'FFmpeg archive did not contain ffmpeg.exe and ffprobe.exe.'
        }

        Copy-Item -LiteralPath $ffmpegSource.FullName -Destination $ffmpegTarget -Force
        Copy-Item -LiteralPath $ffprobeSource.FullName -Destination $ffprobeTarget -Force
        Write-InstallerLog 'FFmpeg and ffprobe installed and verified.'
    }

    $denoTarget = Join-Path $ToolsDirectory 'deno.exe'
    if (-not (Test-Path -LiteralPath $denoTarget)) {
        Write-InstallerLog 'Starting Deno installation.'
        $denoArchiveName = 'deno-x86_64-pc-windows-msvc.zip'
        $denoZip = Join-Path $tempDirectory $denoArchiveName
        $denoChecksumUri = 'https://github.com/denoland/deno/releases/latest/download/' + $denoArchiveName + '.sha256sum'
        $denoChecksumFile = Join-Path $tempDirectory ($denoArchiveName + '.sha256sum')

        Write-InstallerLog 'Downloading Deno checksum file.'
        Download-File -Uri $denoChecksumUri -Destination $denoChecksumFile
        Write-InstallerLog 'Deno checksum file downloaded.'

        $denoChecksumText = [IO.File]::ReadAllText($denoChecksumFile)
        $denoExpected = ([regex]::Match($denoChecksumText, '[a-fA-F0-9]{64}')).Value
        if ([string]::IsNullOrWhiteSpace($denoExpected)) {
            throw 'No SHA-256 checksum was found for Deno.'
        }

        Write-InstallerLog 'Downloading Deno archive.'
        Download-File -Uri ('https://github.com/denoland/deno/releases/latest/download/' + $denoArchiveName) -Destination $denoZip
        Write-InstallerLog 'Deno archive downloaded.'

        Assert-Sha256 -Path $denoZip -ExpectedHash $denoExpected -DisplayName 'Deno archive'

        $denoExtract = Join-Path $tempDirectory 'deno'
        Expand-Archive -LiteralPath $denoZip -DestinationPath $denoExtract -Force
        $denoSource = Get-ChildItem -LiteralPath $denoExtract -Filter 'deno.exe' -File -Recurse | Select-Object -First 1
        if (-not $denoSource) {
            throw 'Deno archive did not contain deno.exe.'
        }

        Copy-Item -LiteralPath $denoSource.FullName -Destination $denoTarget -Force
        Write-InstallerLog 'Deno installed and verified.'
    }

    Write-InstallerLog 'Required tools are ready.'
    exit 0
}
catch {
    Write-InstallerLog ('Installer failed: ' + $_.Exception.Message)
    exit 1
}
finally {
    Write-InstallerLog 'Cleaning temporary installer directory.'
    Remove-Item -LiteralPath $tempDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
