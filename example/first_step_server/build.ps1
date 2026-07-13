[CmdletBinding()]
param(
    [string]$Compiler = "g++",
    [string]$OutputName = "first_step_server.exe"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $scriptDir "src"
$binDir = Join-Path $scriptDir "bin"
$outputPath = Join-Path $binDir $OutputName

if (-not (Test-Path $binDir)) {
    New-Item -ItemType Directory -Path $binDir | Out-Null
}

$sources = Get-ChildItem -Path $srcDir -Filter "*.cpp" | Sort-Object Name | ForEach-Object { $_.FullName }

Write-Host "Building first-step prototype..." -ForegroundColor Cyan
Write-Host "$Compiler $($sources -join ' ') -o $outputPath -lws2_32" -ForegroundColor Yellow

& $Compiler @sources -o $outputPath -lws2_32

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Build succeeded: $outputPath" -ForegroundColor Green
