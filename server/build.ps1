$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildDir = Join-Path $PSScriptRoot "build"
$OutputPath = Join-Path $BuildDir "server.exe"
$EigenRoot = "E:\XboxGames\eigen-3.3.4\eigen-3.3.4"

if (-not (Test-Path $BuildDir)) {
    New-Item -ItemType Directory -Path $BuildDir | Out-Null
}

$Sources = Get-ChildItem (Join-Path $PSScriptRoot "src") -Filter *.cpp | ForEach-Object { $_.FullName }

& "C:\msys64\ucrt64\bin\g++.exe" `
    -std=c++17 `
    -Wall `
    -Wextra `
    -I $EigenRoot `
    @Sources `
    -o $OutputPath `
    -lws2_32

Write-Host "Build succeeded:" $OutputPath
