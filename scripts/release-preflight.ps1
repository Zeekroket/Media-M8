param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,
    [ValidateSet('Source', 'Build', 'Packages')]
    [string]$Stage = 'Packages',
    [switch]$Sign,
    [string]$CertificateSubject = 'CN=Zeekroket'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$distDir = Join-Path $repoRoot 'dist'
$app = Join-Path $distDir 'MediaM8.exe'
$setup = Join-Path $distDir "MediaM8-$Version-win-x64-setup.exe"
$zip = Join-Path $distDir "MediaM8-$Version-win-x64-portable.zip"

function Assert-Equal($Label, $Actual, $Expected) {
    if ($Actual -cne $Expected) {
        throw "${Label} mismatch: expected '$Expected', found '$Actual'."
    }
}

function Assert-PatternValue($Path, $Pattern, $Expected, $Label) {
    $source = Get-Content -LiteralPath $Path -Raw
    $matchesFound = [regex]::Matches($source, $Pattern)
    if ($matchesFound.Count -ne 1) {
        throw "$Label must be defined exactly once in $Path."
    }
    Assert-Equal $Label $matchesFound[0].Groups[1].Value $Expected
}

function Assert-File($Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf) -or
        (Get-Item -LiteralPath $Path).Length -eq 0) {
        throw "Missing or empty release file: $Path"
    }
}

function Assert-FileVersion($Path, $Label) {
    Assert-File $Path
    $info = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($Path)
    $actual = '{0}.{1}.{2}.{3}' -f $info.FileMajorPart, $info.FileMinorPart, $info.FileBuildPart, $info.FilePrivatePart
    Assert-Equal "$Label file version" $actual "$Version.0"
    $product = '{0}.{1}.{2}.{3}' -f $info.ProductMajorPart, $info.ProductMinorPart, $info.ProductBuildPart, $info.ProductPrivatePart
    Assert-Equal "$Label product version" $product "$Version.0"
}

function Assert-Signature($Path) {
    if (-not $Sign) { return }
    $signature = Get-AuthenticodeSignature -FilePath $Path
    if ($signature.Status -ne 'Valid') {
        throw "Invalid signature for $Path : $($signature.Status) $($signature.StatusMessage)"
    }
    Assert-Equal "Signer of $Path" $signature.SignerCertificate.Subject $CertificateSubject
}

function Get-StreamHash($Stream) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Stream))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

# Fail before expensive packaging if source versions disagree.
Assert-PatternValue (Join-Path $repoRoot 'src\MediaM8\AppInfo.cs') 'const\s+string\s+Version\s*=\s*"([^"]+)"' $Version 'AppInfo.Version'
[xml]$project = Get-Content -LiteralPath (Join-Path $repoRoot 'src\MediaM8\MediaM8.csproj') -Raw
foreach ($pair in @(@('Version', $Version), @('FileVersion', "$Version.0"), @('AssemblyVersion', "$Version.0"))) {
    $nodes = @($project.SelectNodes("//PropertyGroup/$($pair[0])"))
    if ($nodes.Count -ne 1) { throw "Project $($pair[0]) must be defined exactly once." }
    Assert-Equal "Project $($pair[0])" $nodes[0].InnerText $pair[1]
}
Assert-PatternValue (Join-Path $repoRoot 'installer\MediaM8.iss') '(?m)^\s*#define\s+AppVersion\s+"([^"]+)"' $Version 'Installer default version'
Assert-PatternValue (Join-Path $repoRoot 'installer\MediaM8.iss') '(?m)^AppVersion=([^\r\n]+)\r? '{#AppVersion}' 'Installer AppVersion'
foreach ($script in @('build-installer.ps1', 'package.ps1', 'package-release.ps1')) {
    Assert-PatternValue (Join-Path $PSScriptRoot $script) '\[string\]\$Version\s*=\s*''([^'']+)''' $Version "$script default version"
}
Write-Host "PASS: source versions ($Version)"
if ($Stage -eq 'Source') { return }

Assert-FileVersion $app 'Application'
Assert-Signature $app
Write-Host 'PASS: application version and requested signature'
if ($Stage -eq 'Build') { return }

Assert-FileVersion $setup 'Installer'
Assert-Signature $setup
Assert-File $zip
Add-Type -AssemblyName System.IO.Compression.FileSystem
$expectedEntries = @('MediaM8.exe', 'portable.flag', 'LICENSE', 'tools/README.txt', 'SHA256SUMS.txt')
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    # Read the archive directly. Do not extract or run executables during preflight.
    $files = @($archive.Entries | Where-Object { -not $_.FullName.EndsWith('/') })
    Assert-Equal 'Portable file count' $files.Count $expectedEntries.Count
    foreach ($entry in $files) {
        if ($expectedEntries -cnotcontains $entry.FullName) {
            throw "Unexpected portable archive entry: $($entry.FullName)"
        }
    }
    foreach ($name in $expectedEntries) {
        if (@($files | Where-Object { $_.FullName -ceq $name }).Count -ne 1) {
            throw "Missing or duplicate portable entry: $name"
        }
    }
    foreach ($entry in $archive.Entries | Where-Object { $_.FullName.EndsWith('/') }) {
        if ($entry.FullName -cne 'tools/') { throw "Unexpected archive directory: $($entry.FullName)" }
    }
    Assert-Equal 'Portable marker size' $archive.GetEntry('portable.flag').Length 0
    $reader = [System.IO.StreamReader]::new($archive.GetEntry('SHA256SUMS.txt').Open())
    try { $manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $hashes = @{}
    foreach ($line in ($manifest -split '\r?\n')) {
        if (-not $line) { continue }
        if ($line -notmatch '^([a-fA-F0-9]{64})  (.+)$') { throw 'Invalid portable SHA256SUMS line.' }
        $digest = $Matches[1].ToLowerInvariant()
        $name = $Matches[2]
        if ($name -ceq 'SHA256SUMS.txt' -or $expectedEntries -cnotcontains $name -or $hashes.ContainsKey($name)) {
            throw "Unexpected or duplicate portable hash entry: $name"
        }
        $hashes.Add($name, $digest)
    }
    Assert-Equal 'Portable hash count' $hashes.Count ($expectedEntries.Count - 1)
    foreach ($name in $expectedEntries | Where-Object { $_ -cne 'SHA256SUMS.txt' }) {
        $stream = $archive.GetEntry($name).Open()
        try { $actual = Get-StreamHash $stream } finally { $stream.Dispose() }
        Assert-Equal "Portable hash of $name" $actual $hashes[$name]
    }
    Assert-Equal 'Packaged application hash' $hashes['MediaM8.exe'] (Get-FileHash -LiteralPath $app -Algorithm SHA256).Hash.ToLowerInvariant()
    Assert-Equal 'Packaged license hash' $hashes['LICENSE'] (Get-FileHash -LiteralPath (Join-Path $repoRoot 'LICENSE') -Algorithm SHA256).Hash.ToLowerInvariant()
}
finally { $archive.Dispose() }

Write-Host 'PASS: portable contents, marker and hashes; installer version and requested signature'
if (-not $Sign) { Write-Warning 'Unsigned local verification only; use -Sign for release preflight.' }
$lines = foreach ($asset in @($setup, $zip)) {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path -Leaf $asset)
}
$manifestPath = Join-Path $distDir "MediaM8-$Version-SHA256SUMS.txt"
$lines | Set-Content -LiteralPath $manifestPath -Encoding ASCII
$lines | ForEach-Object { Write-Host $_ }
Write-Host "PASS: package preflight. Release hashes: $manifestPath"
Write-Host 'Manual installation, update and release checklist still required (RELEASE.md).'
 '{#AppVersion}' 'Installer AppVersion'
foreach ($script in @('build-installer.ps1', 'package.ps1', 'package-release.ps1')) {
    Assert-PatternValue (Join-Path $PSScriptRoot $script) '\[string\]\$Version\s*=\s*''([^'']+)''' $Version "$script default version"
}
Write-Host "PASS: source versions ($Version)"
if ($Stage -eq 'Source') { return }

Assert-FileVersion $app 'Application'
Assert-Signature $app
Write-Host 'PASS: application version and requested signature'
if ($Stage -eq 'Build') { return }

Assert-FileVersion $setup 'Installer'
Assert-Signature $setup
Assert-File $zip
Add-Type -AssemblyName System.IO.Compression.FileSystem
$expectedEntries = @('MediaM8.exe', 'portable.flag', 'LICENSE', 'tools/README.txt', 'SHA256SUMS.txt')
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    # Read the archive directly. Do not extract or run executables during preflight.
    $files = @($archive.Entries | Where-Object { -not $_.FullName.EndsWith('/') })
    Assert-Equal 'Portable file count' $files.Count $expectedEntries.Count
    foreach ($entry in $files) {
        if ($expectedEntries -cnotcontains $entry.FullName) {
            throw "Unexpected portable archive entry: $($entry.FullName)"
        }
    }
    foreach ($name in $expectedEntries) {
        if (@($files | Where-Object { $_.FullName -ceq $name }).Count -ne 1) {
            throw "Missing or duplicate portable entry: $name"
        }
    }
    foreach ($entry in $archive.Entries | Where-Object { $_.FullName.EndsWith('/') }) {
        if ($entry.FullName -cne 'tools/') { throw "Unexpected archive directory: $($entry.FullName)" }
    }
    Assert-Equal 'Portable marker size' $archive.GetEntry('portable.flag').Length 0
    $reader = New-Object System.IO.StreamReader($archive.GetEntry('SHA256SUMS.txt').Open())
    try { $manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $hashes = @{}
    foreach ($line in ($manifest -split '\r?\n')) {
        if (-not $line) { continue }
        if ($line -notmatch '^([a-fA-F0-9]{64})  (.+)$') { throw 'Invalid portable SHA256SUMS line.' }
        $digest = $Matches[1].ToLowerInvariant()
        $name = $Matches[2]
        if ($name -ceq 'SHA256SUMS.txt' -or $expectedEntries -cnotcontains $name -or $hashes.ContainsKey($name)) {
            throw "Unexpected or duplicate portable hash entry: $name"
        }
        $hashes.Add($name, $digest)
    }
    Assert-Equal 'Portable hash count' $hashes.Count ($expectedEntries.Count - 1)
    foreach ($name in $expectedEntries | Where-Object { $_ -cne 'SHA256SUMS.txt' }) {
        $stream = $archive.GetEntry($name).Open()
        try { $actual = Get-StreamHash $stream } finally { $stream.Dispose() }
        Assert-Equal "Portable hash of $name" $actual $hashes[$name]
    }
    Assert-Equal 'Packaged application hash' $hashes['MediaM8.exe'] (Get-FileHash -LiteralPath $app -Algorithm SHA256).Hash.ToLowerInvariant()
    Assert-Equal 'Packaged license hash' $hashes['LICENSE'] (Get-FileHash -LiteralPath (Join-Path $repoRoot 'LICENSE') -Algorithm SHA256).Hash.ToLowerInvariant()
}
finally { $archive.Dispose() }

Write-Host 'PASS: portable contents, marker and hashes; installer version and requested signature'
if (-not $Sign) { Write-Warning 'Unsigned local verification only; use -Sign for release preflight.' }
$lines = foreach ($asset in @($setup, $zip)) {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant(), (Split-Path -Leaf $asset)
}
$manifestPath = Join-Path $distDir "MediaM8-$Version-SHA256SUMS.txt"
$lines | Set-Content -LiteralPath $manifestPath -Encoding ASCII
$lines | ForEach-Object { Write-Host $_ }
Write-Host "PASS: package preflight. Release hashes: $manifestPath"
Write-Host 'Manual installation, update and release checklist still required (RELEASE.md).'
