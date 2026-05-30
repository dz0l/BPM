param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$OutputRoot = "dist"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

& "$repoRoot\scripts\create-placeholder-icons.ps1"

$wapProject = Join-Path $repoRoot "packaging\PrintMaestro.Package\PrintMaestro.Package.wapproj"
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe `
    | Select-Object -First 1

if (-not $msbuild) {
    throw "MSBuild not found. Install Visual Studio 2022 with MSIX Packaging Tools."
}

New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

& $msbuild $wapProject `
    /p:Configuration=$Configuration `
    /p:Platform=$Platform `
    /p:AppxPackageDir="$OutputRoot\" `
    /p:UapAppxPackageBuildMode=StoreUpload

$msix = Get-ChildItem -Path $OutputRoot -Filter "*.msix" -Recurse |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $msix) {
    throw "MSIX package was not produced. Check MSIX tooling installation."
}

$version = (Select-Xml -Path "Directory.Build.props" -XPath "//Version").Node.InnerText
$target = Join-Path $OutputRoot "PrintMaestro-$version-$Platform.msix"
Copy-Item $msix.FullName $target -Force
Write-Host "Created $target"
