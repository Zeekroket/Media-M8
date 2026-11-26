param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '0.3.0',
    [switch]$Sign,
    [string]$CertificateSubject = 'CN=Zeekroket',
    [string]$TimestampServer = 'http://timestamp.digicert.com',
    [string]$CompilerPath
)

$ErrorActionPreference = 'Stop'

$preflight = @{
    Version = $Version
    Sign = $Sign
    CertificateSubject = $CertificateSubject
}

& (Join-Path $PSScriptRoot 'release-preflight.ps1') @preflight -Stage Build
& (Join-Path $PSScriptRoot 'test.ps1')

$installerArgs = @{
    Version = $Version
    Sign = $Sign
    CertificateSubject = $CertificateSubject
    TimestampServer = $TimestampServer
}
if ($CompilerPath) {
    $installerArgs.CompilerPath = $CompilerPath
}

& (Join-Path $PSScriptRoot 'build-installer.ps1') @installerArgs
& (Join-Path $PSScriptRoot 'package.ps1') -Version $Version

$distDir = Join-Path (Split-Path -Parent $PSScriptRoot) 'dist'
$assets = @(
    (Join-Path $distDir "MediaM8-$Version-win-x64-setup.exe"),
    (Join-Path $distDir "MediaM8-$Version-win-x64-portable.zip")
)

foreach ($asset in $assets) {
    if (-not (Test-Path -LiteralPath $asset -PathType Leaf)) {
        throw "Release asset missing: $asset"
    }
}
& (Join-Path $PSScriptRoot 'release-preflight.ps1') @preflight -Stage Packages
Write-Host 'Both release distributions passed package preflight:'
$assets | ForEach-Object { Write-Host $_ }
