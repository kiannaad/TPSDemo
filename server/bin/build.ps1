[CmdletBinding()]
param(
    [string]$Compiler = "g++",
    [string]$OutputName = "main.exe"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverDir = Split-Path -Parent $scriptDir
$srcDir = Join-Path $serverDir "src"
$outputPath = Join-Path $scriptDir $OutputName

$sourceFiles = Get-ChildItem -Path $srcDir -Filter "*.cpp" | Sort-Object Name

if (-not $sourceFiles) {
    throw "No .cpp files found in $srcDir"
}

$sourcePaths = $sourceFiles | ForEach-Object { $_.FullName }

Write-Host "Building sources:" -ForegroundColor Cyan
$sourceFiles | ForEach-Object { Write-Host "  $($_.Name)" }

$arguments = @()
$arguments += $sourcePaths
$arguments += "-o"
$arguments += $outputPath
$arguments += "-lws2_32"

Write-Host ""
Write-Host "$Compiler $($arguments -join ' ')" -ForegroundColor Yellow

& $Compiler @arguments

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Build succeeded: $outputPath" -ForegroundColor Green
