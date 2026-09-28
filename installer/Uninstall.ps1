param(
    [switch]$RemoveUserData,
    [switch]$Silent
)

$ErrorActionPreference = "Stop"
$installDir = Join-Path $env:ProgramFiles "PeekPin"
$previousDir = Join-Path $env:LOCALAPPDATA "Programs\PeekPin"

Get-Process PeekPin -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400

$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
if (Test-Path $runKey) {
    Remove-ItemProperty -Path $runKey -Name "PeekPin" -ErrorAction SilentlyContinue
}

$shortcuts = @(
    (Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs\PeekPin.lnk"),
    (Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\PeekPin.lnk")
)
foreach ($shortcutPath in $shortcuts) {
    if (Test-Path $shortcutPath) {
        Remove-Item $shortcutPath -Force
    }
}

$uninstallKeys = @(
    "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\PeekPin",
    "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\PeekPin"
)
foreach ($uninstallKey in $uninstallKeys) {
    if (Test-Path $uninstallKey) {
        Remove-Item $uninstallKey -Recurse -Force
    }
}

foreach ($dir in @($installDir, $previousDir)) {
    if (Test-Path $dir) {
        Remove-Item $dir -Recurse -Force
    }
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
