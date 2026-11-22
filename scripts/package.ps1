param(
    [string]$Version = '0.1.0'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$distDir = Join-Path $repoRoot 'dist'
$executable = Join-Path $distDir 'MediaM8.exe'
$script = Join-Path $repoRoot 'src\MediaM8.ps1'
$runtimeToolsScript = Join-Path $repoRoot 'src\RuntimeTools.ps1'
$installerScript = Join-Path $repoRoot 'src\InstallRuntimeTools.ps1'
$icon = Join-Path $repoRoot 'assets\MediaM8.ico'
$packageRoot = Join-Path $distDir "MediaM8-$Version-win-x64"
$zipPath = Join-Path $distDir "MediaM8-$Version-win-x64.zip"

if (-not (Test-Path -LiteralPath $executable)) { throw 'dist\MediaM8.exe is missing. Build the launcher first.' }
if (-not (Test-Path -LiteralPath $script)) { throw 'src\MediaM8.ps1 is missing.' }
if (-not (Test-Path -LiteralPath $runtimeToolsScript)) { throw 'src\RuntimeTools.ps1 is missing.' }
if (-not (Test-Path -LiteralPath $installerScript)) { throw 'src\InstallRuntimeTools.ps1 is missing.' }
if (-not (Test-Path -LiteralPath $icon)) { throw 'assets\MediaM8.ico is missing.' }

Remove-Item -LiteralPath $packageRoot -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'tools') -Force | Out-Null

Copy-Item -LiteralPath $executable -Destination $packageRoot
Copy-Item -LiteralPath $script -Destination (Join-Path $packageRoot 'MediaM8.ps1')
Copy-Item -LiteralPath $runtimeToolsScript -Destination (Join-Path $packageRoot 'RuntimeTools.ps1')
Copy-Item -LiteralPath $installerScript -Destination (Join-Path $packageRoot 'InstallRuntimeTools.ps1')
Copy-Item -LiteralPath $icon -Destination (Join-Path $packageRoot 'MediaM8.ico')
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $packageRoot

$toolsReadme = @(
    'Media M8 runtime tools',
    '',
    'Place these files in this folder:',
    '- yt-dlp.exe',
    '- ffmpeg.exe',
    '- ffprobe.exe',
    '- deno.exe (recommended for full YouTube support)',
    '',
    'If the required tools are missing, Media M8 can offer to download them from the official project releases.'
)
$toolsReadme | Set-Content -LiteralPath (Join-Path $packageRoot 'tools\README.txt') -Encoding UTF8

$hashLines = @()
foreach ($file in Get-ChildItem -LiteralPath $packageRoot -File -Recurse) {
    $relativePath = $file.FullName.Substring($packageRoot.Length + 1).Replace('\', '/')
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $hashLines += "$hash  $relativePath"
}
$hashLines | Set-Content -LiteralPath (Join-Path $packageRoot 'SHA256SUMS.txt') -Encoding ASCII

Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $zipPath -CompressionLevel Optimal
Write-Host "Packaged $zipPath"
Get-FileHash -LiteralPath $zipPath -Algorithm SHA256 | Format-List
