$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repoRoot 'src\launcher\MediaM8Launcher.cpp'
$definition = Join-Path $repoRoot 'src\launcher\kernel32.def'
$buildDir = Join-Path $repoRoot 'build\launcher'
$distDir = Join-Path $repoRoot 'dist'
$object = Join-Path $buildDir 'MediaM8Launcher.obj'
$kernelLibrary = Join-Path $buildDir 'kernel32.lib'
$output = Join-Path $distDir 'MediaM8.exe'

$clang = Get-Command 'clang++.exe' -ErrorAction SilentlyContinue
$lld = Get-Command 'lld-link.exe' -ErrorAction SilentlyContinue

if (-not $clang -or -not $lld) {
    throw 'LLVM clang++ and lld-link are required to build the native launcher.'
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

$linkArgs = @(
    '/entry:entry',
    '/subsystem:windows',
    '/machine:x64',
    '/nodefaultlib',
    "/out:$output",
    $object,
    $kernelLibrary
)
& $lld.Source @linkArgs
if ($LASTEXITCODE -ne 0) { throw "Failed to link the launcher (exit code $LASTEXITCODE)." }

Write-Host "Built $output"
Get-FileHash -Path $output -Algorithm SHA256 | Format-List
