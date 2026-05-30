param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$imagesDir = Join-Path $repoRoot "packaging\PrintMaestro.Package\Images"
New-Item -ItemType Directory -Force -Path $imagesDir | Out-Null

Add-Type -AssemblyName System.Drawing

function New-PlaceholderIcon {
    param([int]$Width, [int]$Height, [string]$Path)
    $bitmap = New-Object System.Drawing.Bitmap $Width, $Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 59, 130, 246))
    $graphics.Dispose()
    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

New-PlaceholderIcon -Width 50 -Height 50 -Path (Join-Path $imagesDir "StoreLogo.png")
New-PlaceholderIcon -Width 44 -Height 44 -Path (Join-Path $imagesDir "Square44x44Logo.png")
New-PlaceholderIcon -Width 150 -Height 150 -Path (Join-Path $imagesDir "Square150x150Logo.png")
New-PlaceholderIcon -Width 310 -Height 150 -Path (Join-Path $imagesDir "Wide310x150Logo.png")

Write-Host "Placeholder MSIX icons created in $imagesDir"
