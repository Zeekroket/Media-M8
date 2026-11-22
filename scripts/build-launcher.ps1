param(
    [switch]$Sign,
    [string]$CertificateSubject = 'CN=Zeekroket',
    [string]$TimestampServer = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repoRoot 'src\launcher\MediaM8Launcher.cpp'
$definition = Join-Path $repoRoot 'src\launcher\kernel32.def'
$resourceDefinition = Join-Path $repoRoot 'assets\MediaM8.rc'
$buildDir = Join-Path $repoRoot 'build\launcher'
$distDir = Join-Path $repoRoot 'dist'
$object = Join-Path $buildDir 'MediaM8Launcher.obj'
$kernelLibrary = Join-Path $buildDir 'kernel32.lib'
$resourceObject = Join-Path $buildDir 'MediaM8.res'
$output = Join-Path $distDir 'MediaM8.exe'

$clang = Get-Command 'clang++.exe' -ErrorAction SilentlyContinue
$lld = Get-Command 'lld-link.exe' -ErrorAction SilentlyContinue
$llvmRc = Get-Command 'llvm-rc.exe' -ErrorAction SilentlyContinue

if (-not $clang -or -not $lld -or -not $llvmRc) {
    throw 'LLVM clang++, lld-link and llvm-rc are required to build the native launcher.'
}

New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

$libArgs = @('/lib', '/machine:x64', "/def:$definition", "/out:$kernelLibrary")
& $lld.Source @libArgs
if ($LASTEXITCODE -ne 0) { throw "Failed to create the import library (exit code $LASTEXITCODE)." }

$compileArgs = @(
    '-target', 'x86_64-pc-windows-msvc',
    '-fno-exceptions',
    '-fno-rtti',
    '-fno-stack-protector',
    '-fno-builtin',
    '-ffreestanding',
    '-c', $source,
    '-o', $object
)
& $clang.Source @compileArgs
if ($LASTEXITCODE -ne 0) { throw "Failed to compile the launcher (exit code $LASTEXITCODE)." }

Push-Location (Join-Path $repoRoot 'assets')
try {
    & $llvmRc.Source "/fo$resourceObject" $resourceDefinition
    if ($LASTEXITCODE -ne 0) { throw "Failed to compile the icon resource (exit code $LASTEXITCODE)." }
}
finally {
    Pop-Location
}

$linkArgs = @(
    '/entry:entry',
    '/subsystem:windows',
    '/machine:x64',
    '/nodefaultlib',
    "/out:$output",
    $object,
    $resourceObject,
    $kernelLibrary
)
& $lld.Source @linkArgs
if ($LASTEXITCODE -ne 0) { throw "Failed to link the launcher (exit code $LASTEXITCODE)." }

Write-Host "Built $output"

if ($Sign) {
    $certificate = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
        Where-Object { $_.Subject -eq $CertificateSubject -and $_.HasPrivateKey } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

    if (-not $certificate) {
        throw "No code-signing certificate with private key was found for '$CertificateSubject'."
    }

    Write-Host "Signing with $($certificate.Subject) [$($certificate.Thumbprint)]"

    $signature = Set-AuthenticodeSignature `
        -FilePath $output `
        -Certificate $certificate `
        -HashAlgorithm SHA256 `
        -TimestampServer $TimestampServer

    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Signing failed: $($signature.Status) - $($signature.StatusMessage)"
    }

    $verified = Get-AuthenticodeSignature -FilePath $output
    if ($verified.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Signature verification failed: $($verified.Status) - $($verified.StatusMessage)"
    }

    Write-Host 'Signature verified.'
}

Get-FileHash -Path $output -Algorithm SHA256 | Format-List
