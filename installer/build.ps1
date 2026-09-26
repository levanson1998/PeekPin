$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$publish = Join-Path $root "artifacts\publish"
$dist = Join-Path $root "dist\PeekPin-1.0.0-win-x64"
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

Get-Process PeekPin -ErrorAction SilentlyContinue | Stop-Process -Force
if (Test-Path $publish) {
    Remove-Item $publish -Recurse -Force
}
if (Test-Path $dist) {
    Remove-Item $dist -Recurse -Force
}

& $dotnet publish (Join-Path $root "src\PeekPin.App\PeekPin.App.csproj") -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $publish
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

New-Item -ItemType Directory -Force -Path (Join-Path $dist "app") | Out-Null
Copy-Item -Path (Join-Path $publish "*") -Destination (Join-Path $dist "app") -Recurse
Copy-Item (Join-Path $PSScriptRoot "Install.ps1") $dist
Copy-Item (Join-Path $PSScriptRoot "Uninstall.ps1") $dist
Copy-Item (Join-Path $PSScriptRoot "Setup.cmd") $dist
Write-Host "Setup folder: $dist"
