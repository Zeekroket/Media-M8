param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '0.1.0',
    [switch]$Sign,
    [string]$CertificateSubject = 'CN=Zeekroket',
    [string]$TimestampServer = 'http://timestamp.digicert.com',
    [string]$CompilerPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$distDir = Join-Path $repoRoot 'dist'
$executable = Join-Path $distDir 'MediaM8.exe'
$setupScript = Join-Path $repoRoot 'installer\MediaM8.iss'
$setupExe = Join-Path $distDir "MediaM8-$Version-win-x64-setup.exe"

if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw 'dist\MediaM8.exe is missing. Build Media M8 first.'
}

if (-not $CompilerPath) {
    $command = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($command) {
        $CompilerPath = $command.Source
    } else {
        $roots = @(
            [Environment]::GetFolderPath('ProgramFilesX86'),
            [Environment]::GetFolderPath('ProgramFiles'),
            [Environment]::GetFolderPath('LocalApplicationData')
        )
        foreach ($root in $roots) {
            if (-not $root) { continue }
            foreach ($relative in @('Inno Setup 6\ISCC.exe', 'Programs\Inno Setup 6\ISCC.exe')) {
                $candidate = Join-Path $root $relative
                if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                    $CompilerPath = $candidate
                    break
                }
            }
            if ($CompilerPath) { break }
        }
    }
}

if (-not $CompilerPath -or -not (Test-Path -LiteralPath $CompilerPath -PathType Leaf)) {
    throw 'Inno Setup 6.3 or newer is required. Install it from https://jrsoftware.org/isdl.php or supply -CompilerPath.'
}

if ($Sign) {
    $appSignature = Get-AuthenticodeSignature -FilePath $executable
    if ($appSignature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw 'MediaM8.exe is not validly signed. Run build.ps1 -Sign first.'
    }
    $certificate = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
        Where-Object { $_.Subject -eq $CertificateSubject -and $_.HasPrivateKey } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1
    if (-not $certificate) {
        throw "No code-signing certificate with private key was found for '$CertificateSubject'."
    }
}

if (Test-Path -LiteralPath $setupExe) {
    Remove-Item -LiteralPath $setupExe -Force
}

& $CompilerPath "/DAppVersion=$Version" $setupScript
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $setupExe -PathType Leaf)) {
    throw "Installer compilation failed (exit code $LASTEXITCODE)."
}

if ($Sign) {
    $signature = Set-AuthenticodeSignature -FilePath $setupExe -Certificate $certificate -HashAlgorithm SHA256 -TimestampServer $TimestampServer
    $verified = Get-AuthenticodeSignature -FilePath $setupExe
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid -or
        $verified.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Installer signing failed: $($signature.Status) - $($signature.StatusMessage)"
    }
    Write-Host 'Installer signature verified.'
}

Write-Host "Built installer $setupExe"
Get-FileHash -LiteralPath $setupExe -Algorithm SHA256 | Format-List
