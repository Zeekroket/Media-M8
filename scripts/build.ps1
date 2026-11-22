param(
    [switch]$Sign,
    [string]$CertificateSubject = 'CN=Zeekroket',
    [string]$TimestampServer = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\MediaM8\MediaM8.csproj'
$distDir = Join-Path $repoRoot 'dist'
$output = Join-Path $distDir 'MediaM8.exe'

$dotnet = Get-Command 'dotnet.exe' -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw '.NET 10 SDK is required to build Media M8.'
}

$sdkVersionText = (& $dotnet.Source --version).Trim()
$sdkVersion = $null
if (-not [Version]::TryParse(($sdkVersionText -split '-')[0], [ref]$sdkVersion) -or $sdkVersion.Major -lt 10) {
    throw ".NET 10 SDK or newer is required. Found: $sdkVersionText"
}

if (-not (Test-Path -LiteralPath $project)) {
    throw "Project file is missing: $project"
}

New-Item -ItemType Directory -Path $distDir -Force | Out-Null

@(
    'MediaM8.ps1',
    'RuntimeTools.ps1',
    'InstallRuntimeTools.ps1',
    'MediaM8.ico'
) | ForEach-Object {
    Remove-Item -LiteralPath (Join-Path $distDir $_) -Force -ErrorAction SilentlyContinue
}

$publishArgs = @(
    'publish',
    $project,
    '--configuration', 'Release',
    '--runtime', 'win-x64',
    '--self-contained', 'true',
    '--output', $distDir,
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:PublishTrimmed=false'
)

& $dotnet.Source @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $output)) {
    throw "Build succeeded but $output was not created."
}

Write-Host "Built $output"
Write-Host 'Runtime: self-contained .NET 10; PowerShell is not required to run Media M8.'

if ($Sign) {
    $certificate = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
        Where-Object { $_.Subject -eq $CertificateSubject -and $_.HasPrivateKey } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if (-not $certificate) {
        throw "No code-signing certificate with private key was found for '$CertificateSubject'."
    }

    Write-Host "Signing with $($certificate.Subject) [$($certificate.Thumbprint)]"

    $signArgs = @{
        FilePath = $output
        Certificate = $certificate
        HashAlgorithm = 'SHA256'
        TimestampServer = $TimestampServer
    }
    $signature = Set-AuthenticodeSignature @signArgs

    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Signing failed: $($signature.Status) - $($signature.StatusMessage)"
    }

    $verified = Get-AuthenticodeSignature -FilePath $output
    if ($verified.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Signature verification failed: $($verified.Status) - $($verified.StatusMessage)"
    }

    Write-Host 'Signature verified.'
}

Get-FileHash -LiteralPath $output -Algorithm SHA256 | Format-List
