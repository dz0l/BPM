param(
    [string]$SourcePng = ""
)

$repoRoot = Split-Path -Parent $PSScriptRoot
& "$repoRoot\scripts\generate-icons.ps1" -SourcePng $SourcePng
