param(
    [string]$Version = '0.2.0'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$distDir = Join-Path $repoRoot 'dist'
$executable = Join-Path $distDir 'MediaM8.exe'
$packageRoot = Join-Path $distDir "MediaM8-$Version-win-x64-portable"
$zipPath = Join-Path $distDir "MediaM8-$Version-win-x64-portable.zip"

if (-not (Test-Path -LiteralPath $executable)) {
    throw 'dist\MediaM8.exe is missing. Build Media M8 first.'
}

Remove-Item -LiteralPath $packageRoot -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'tools') -Force | Out-Null

Copy-Item -LiteralPath $executable -Destination $packageRoot
New-Item -ItemType File -Path (Join-Path $packageRoot 'portable.flag') | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $packageRoot

$toolsReadme = @(
    'Media M8 runtime tools',
    '',
    'Media M8 manages these portable runtime tools in this folder:',
    '- yt-dlp.exe',
    '- ffmpeg.exe',
    '- ffprobe.exe',
    '- deno.exe',
    '',
    'If they are missing, Media M8 can download them from the official project releases and verify their SHA-256 checksums.'
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
