param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = "dist"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Add-Type -AssemblyName System.IO.Compression.FileSystem

function New-ZipFromDirectory {
    param(
        [string]$SourceDir,
        [string]$ZipPath,
        [int]$MaxRetries = 5
    )

    for ($attempt = 1; $attempt -le $MaxRetries; $attempt++) {
        try {
            if (Test-Path $ZipPath) {
                Remove-Item $ZipPath -Force
            }

            [System.IO.Compression.ZipFile]::CreateFromDirectory($SourceDir, $ZipPath)
            return
        }
        catch {
            if ($attempt -eq $MaxRetries) {
                throw
            }

            Start-Sleep -Seconds 2
        }
    }
}

$version = (Select-Xml -Path "Directory.Build.props" -XPath "//Version").Node.InnerText
$publishDir = Join-Path $OutputRoot "PrintMaestro-$version"
$zipPath = Join-Path $OutputRoot "PrintMaestro-$version-win-x64.zip"

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

dotnet restore PrintMaestro.slnx -r win-x64

dotnet publish src/PrintMaestro/PrintMaestro.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o $publishDir

dotnet publish src/PrintMaestro.PdfWorker/PrintMaestro.PdfWorker.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o (Join-Path $publishDir "workers\pdf")

dotnet publish src/PrintMaestro.OfficeWorker/PrintMaestro.OfficeWorker.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o (Join-Path $publishDir "workers\office")

dotnet publish src/PrintMaestro.ImageWorker/PrintMaestro.ImageWorker.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o (Join-Path $publishDir "workers\image")

Start-Sleep -Seconds 1

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

$zipFullPath = Join-Path (Get-Location) $zipPath
New-ZipFromDirectory -SourceDir (Resolve-Path $publishDir).Path -ZipPath $zipFullPath

if (-not (Test-Path $zipPath)) {
    throw "ZIP was not created: $zipPath"
}

Write-Host "Created $zipPath"

$hash = Get-FileHash -Path $zipPath -Algorithm SHA256
$checksumPath = "$zipPath.sha256"
Set-Content -Path $checksumPath -Value ("{0}  {1}" -f $hash.Hash, (Split-Path $zipPath -Leaf)) -NoNewline
Write-Host "Created $checksumPath"
