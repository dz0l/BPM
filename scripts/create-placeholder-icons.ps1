param(
    [string]$SourcePng = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$imagesDir = Join-Path $repoRoot "packaging\PrintMaestro.Package\Images"
New-Item -ItemType Directory -Force -Path $imagesDir | Out-Null

if (-not $SourcePng) {
    $SourcePng = Join-Path $repoRoot "src\PrintMaestro\Assets\Icons\app.png"
}

if (-not (Test-Path $SourcePng)) {
    throw "Source PNG not found: $SourcePng"
}

Add-Type -AssemblyName System.Drawing

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

$sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path $SourcePng).Path)

Save-ResizedPng -Source $sourceImage -Width 50 -Height 50 -Path (Join-Path $imagesDir "StoreLogo.png")
Save-ResizedPng -Source $sourceImage -Width 44 -Height 44 -Path (Join-Path $imagesDir "Square44x44Logo.png")
Save-ResizedPng -Source $sourceImage -Width 150 -Height 150 -Path (Join-Path $imagesDir "Square150x150Logo.png")
Save-ResizedPng -Source $sourceImage -Width 310 -Height 150 -Path (Join-Path $imagesDir "Wide310x150Logo.png") -CenterOnCanvas

$sourceImage.Dispose()

Write-Host "MSIX icons created in $imagesDir from $SourcePng"
