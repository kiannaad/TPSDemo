[CmdletBinding()]
param(
    [string]$Compiler = "g++",
    [string]$OutputName = "winsock2_http_demo.exe"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $scriptDir "main.cpp"
$outputPath = Join-Path $scriptDir $OutputName

Write-Host "Building isolated WinSock2 example..." -ForegroundColor Cyan
Write-Host "$Compiler $sourcePath -o $outputPath -lws2_32" -ForegroundColor Yellow

& $Compiler $sourcePath -o $outputPath -lws2_32

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Build succeeded: $outputPath" -ForegroundColor Green
