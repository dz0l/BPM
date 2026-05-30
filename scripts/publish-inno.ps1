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
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)

$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    $registryPaths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1"
    )

    foreach ($key in $registryPaths) {
        $installDir = (Get-ItemProperty -Path $key -ErrorAction SilentlyContinue).InstallLocation
        if ($installDir) {
            $candidate = Join-Path $installDir.TrimEnd('\') "ISCC.exe"
            if (Test-Path $candidate) {
                $iscc = $candidate
                break
            }
        }
    }
}

if (-not $iscc) {
    throw @"
Inno Setup 6 (ISCC.exe) not found.
Install: winget install JRSoftware.InnoSetup
Or:      winget install --id JRSoftware.InnoSetup -e
Or:      choco install innosetup -y
"@
}

$appVersionArg = "/DAppVersion=$version"

Write-Host "Using ISCC: $iscc"
Write-Host "Publish source: $(Join-Path $repoRoot $publishDir)"

& $iscc $issPath $appVersionArg

$setupPath = Join-Path $OutputRoot "PrintMaestro-$version-Setup.exe"
if (-not (Test-Path $setupPath)) {
    throw "Inno Setup did not produce: $setupPath"
}

Write-Host "Created $setupPath"

$hash = Get-FileHash -Path $setupPath -Algorithm SHA256
$checksumPath = "$setupPath.sha256"
Set-Content -Path $checksumPath -Value ("{0}  {1}" -f $hash.Hash, (Split-Path $setupPath -Leaf)) -NoNewline
Write-Host "Created $checksumPath"
