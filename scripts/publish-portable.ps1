param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = "dist"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$version = (Select-Xml -Path "Directory.Build.props" -XPath "//Version").Node.InnerText
$publishDir = Join-Path $OutputRoot "PrintMaestro-$version"
$zipPath = Join-Path $OutputRoot "PrintMaestro-$version-win-x64.zip"

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

dotnet publish src/PrintMaestro/PrintMaestro.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o $publishDir

dotnet publish src/PrintMaestro.PdfWorker/PrintMaestro.PdfWorker.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o (Join-Path $publishDir "workers")

dotnet publish src/PrintMaestro.OfficeWorker/PrintMaestro.OfficeWorker.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o (Join-Path $publishDir "workers")

dotnet publish src/PrintMaestro.ImageWorker/PrintMaestro.ImageWorker.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o (Join-Path $publishDir "workers")

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath
Write-Host "Created $zipPath"
