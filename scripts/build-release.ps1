param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = "dist"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Write-Host "=== Print Maestro release build ===" -ForegroundColor Cyan

& "$repoRoot\scripts\publish-inno.ps1" -Configuration $Configuration -OutputRoot $OutputRoot

$version = (Select-Xml -Path "Directory.Build.props" -XPath "//Version").Node.InnerText
$dist = Resolve-Path $OutputRoot

Write-Host ""
Write-Host "Release $version artifacts in $dist" -ForegroundColor Green
Get-ChildItem -Path $OutputRoot -Filter "PrintMaestro-$version*" | ForEach-Object {
    Write-Host "  $($_.Name) ($([math]::Round($_.Length / 1MB, 2)) MB)"
}

Write-Host ""
Write-Host "Publish to GitHub:" -ForegroundColor Yellow
Write-Host "  git tag v$version"
Write-Host "  git push origin v$version"
