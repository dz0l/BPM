param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = "dist"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

& "$repoRoot\scripts\publish-portable.ps1" -Configuration $Configuration -OutputRoot $OutputRoot

$version = (Select-Xml -Path "Directory.Build.props" -XPath "//Version").Node.InnerText
$publishDir = Join-Path $OutputRoot "PrintMaestro-$version"
$issPath = Join-Path $repoRoot "packaging\inno\PrintMaestro.iss"

if (-not (Test-Path $publishDir)) {
    throw "Publish directory not found: $publishDir"
}

$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
)

$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    throw @"
Inno Setup 6 (ISCC.exe) not found.
Install: winget install JRSoftware.InnoSetup
Or:      choco install innosetup -y
"@
}

$publishSourceArg = "/DPublishSource=$publishDir"
$appVersionArg = "/DAppVersion=$version"

& $iscc $issPath $publishSourceArg $appVersionArg

$setupPath = Join-Path $OutputRoot "PrintMaestro-$version-Setup.exe"
if (-not (Test-Path $setupPath)) {
    throw "Inno Setup did not produce: $setupPath"
}

Write-Host "Created $setupPath"
