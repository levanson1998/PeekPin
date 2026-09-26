param(
    [switch]$RemoveUserData,
    [switch]$Silent
)

$ErrorActionPreference = "Stop"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\PeekPin"

Get-Process PeekPin -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400

$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
if (Test-Path $runKey) {
    Remove-ItemProperty -Path $runKey -Name "PeekPin" -ErrorAction SilentlyContinue
}

$shortcutPath = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\PeekPin.lnk"
if (Test-Path $shortcutPath) {
    Remove-Item $shortcutPath -Force
}

$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\PeekPin"
if (Test-Path $uninstallKey) {
    Remove-Item $uninstallKey -Recurse -Force
}

if (Test-Path $installDir) {
    Remove-Item $installDir -Recurse -Force
}

if ($RemoveUserData) {
    $dataDir = Join-Path $env:APPDATA "PeekPin"
    if (Test-Path $dataDir) {
        Remove-Item $dataDir -Recurse -Force
    }
}

if (-not $Silent) {
    Write-Host "PeekPin uninstalled."
}
