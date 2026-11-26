$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'tests\MediaM8.Regression\MediaM8.Regression.csproj'

& dotnet run --project $project --configuration Release
if ($LASTEXITCODE -ne 0) {
    throw "Media M8 regression tests failed (exit code $LASTEXITCODE)."
}
