param(
    [string]$SourcePng = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$iconsDir = Join-Path $repoRoot "src\PrintMaestro\Assets\Icons"
$msixDir = Join-Path $repoRoot "packaging\PrintMaestro.Package\Images"

if (-not $SourcePng) {
    $SourcePng = Join-Path $iconsDir "app.png"
}

if (-not (Test-Path $SourcePng)) {
    throw "Source PNG not found: $SourcePng"
}

Add-Type -AssemblyName System.Drawing

function Get-AlphaBounds {
    param([System.Drawing.Bitmap]$Bitmap, [byte]$AlphaThreshold = 16)

    $minX = $Bitmap.Width
    $minY = $Bitmap.Height
    $maxX = 0
    $maxY = 0

    for ($y = 0; $y -lt $Bitmap.Height; $y++) {
        for ($x = 0; $x -lt $Bitmap.Width; $x++) {
            if ($Bitmap.GetPixel($x, $y).A -gt $AlphaThreshold) {
                if ($x -lt $minX) { $minX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }

    if ($maxX -lt $minX -or $maxY -lt $minY) {
        throw "PNG has no visible pixels: $SourcePng"
    }

    return @{
        X = $minX
        Y = $minY
        Width = ($maxX - $minX + 1)
        Height = ($maxY - $minY + 1)
    }
}

function Save-ResizedPng {
    param(
        [System.Drawing.Image]$Source,
        [int]$Width,
        [int]$Height,
        [string]$Path,
        [switch]$CenterOnCanvas
    )

    $bitmap = New-Object System.Drawing.Bitmap $Width, $Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    if ($CenterOnCanvas) {
        $scale = [Math]::Min($Width / $Source.Width, $Height / $Source.Height)
        $drawWidth = [int][Math]::Round($Source.Width * $scale)
        $drawHeight = [int][Math]::Round($Source.Height * $scale)
        $x = [int](($Width - $drawWidth) / 2)
        $y = [int](($Height - $drawHeight) / 2)
        $graphics.DrawImage($Source, $x, $y, $drawWidth, $drawHeight)
    }
    else {
        $graphics.DrawImage($Source, 0, 0, $Width, $Height)
    }

    $graphics.Dispose()
    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

$sourcePath = (Resolve-Path $SourcePng).Path
$sourceBitmap = New-Object System.Drawing.Bitmap $sourcePath
$bounds = Get-AlphaBounds -Bitmap $sourceBitmap

$trimmed = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
$trimGraphics = [System.Drawing.Graphics]::FromImage($trimmed)
$trimGraphics.Clear([System.Drawing.Color]::Transparent)
$trimGraphics.DrawImage(
    $sourceBitmap,
    (New-Object System.Drawing.Rectangle 0, 0, $bounds.Width, $bounds.Height),
    (New-Object System.Drawing.Rectangle $bounds.X, $bounds.Y, $bounds.Width, $bounds.Height),
    [System.Drawing.GraphicsUnit]::Pixel)
$trimGraphics.Dispose()
$sourceBitmap.Dispose()

$trimmedPath = Join-Path $iconsDir "app-trimmed.png"
$trimmed.Save($trimmedPath, [System.Drawing.Imaging.ImageFormat]::Png)

Save-ResizedPng -Source $trimmed -Width 24 -Height 24 -Path (Join-Path $iconsDir "app-titlebar.png")
Save-ResizedPng -Source $trimmed -Width 32 -Height 32 -Path (Join-Path $iconsDir "app-titlebar-32.png")

New-Item -ItemType Directory -Force -Path $msixDir | Out-Null
Save-ResizedPng -Source $trimmed -Width 50 -Height 50 -Path (Join-Path $msixDir "StoreLogo.png")
Save-ResizedPng -Source $trimmed -Width 44 -Height 44 -Path (Join-Path $msixDir "Square44x44Logo.png")
Save-ResizedPng -Source $trimmed -Width 150 -Height 150 -Path (Join-Path $msixDir "Square150x150Logo.png")
Save-ResizedPng -Source $trimmed -Width 310 -Height 150 -Path (Join-Path $msixDir "Wide310x150Logo.png") -CenterOnCanvas

$trimmed.Dispose()

$magick = Get-Command magick -ErrorAction SilentlyContinue
$icoPath = Join-Path $iconsDir "app.ico"
if ($magick) {
    & magick $SourcePng -define icon:auto-resize=256,128,96,64,48,32,24,16 $icoPath
    Write-Host "Regenerated $icoPath via ImageMagick"
}
else {
    Write-Host "ImageMagick not found - regenerate app.ico manually (PNG alpha, sizes 16-256)." -ForegroundColor Yellow
}

Write-Host "Generated:"
Write-Host "  $trimmedPath"
Write-Host "  $(Join-Path $iconsDir 'app-titlebar.png')"
Write-Host "  MSIX tiles in $msixDir"
