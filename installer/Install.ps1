param(
    [string]$Source,
    [switch]$Silent
)

$ErrorActionPreference = "Stop"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\PeekPin"
$sourceDir = if ($Source) { $Source } else { Join-Path $PSScriptRoot "app" }
$exe = Join-Path $sourceDir "PeekPin.exe"

if (-not (Test-Path $exe)) {
    throw "PeekPin.exe was not found in $sourceDir"
}

Get-Process PeekPin -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item -Path (Join-Path $sourceDir "*") -Destination $installDir -Recurse -Force
Copy-Item -Path (Join-Path $PSScriptRoot "Uninstall.ps1") -Destination (Join-Path $installDir "Uninstall.ps1") -Force

$uninstallCmd = @"
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall.ps1" %*
"@
Set-Content -Path (Join-Path $installDir "Uninstall.cmd") -Value $uninstallCmd -Encoding ASCII

$startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
$shortcutPath = Join-Path $startMenu "PeekPin.lnk"
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $installDir "PeekPin.exe"
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "PeekPin"
$shortcut.Save()

$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\PeekPin"
New-Item -Path $uninstallKey -Force | Out-Null
$sizeKb = [int]((Get-ChildItem $installDir -Recurse -File | Measure-Object Length -Sum).Sum / 1KB)
$uninstallCommand = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$installDir\Uninstall.ps1`""
New-ItemProperty -Path $uninstallKey -Name DisplayName -Value "PeekPin" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name DisplayVersion -Value "1.0.0" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name Publisher -Value "Le Van Son" -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name InstallLocation -Value $installDir -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name DisplayIcon -Value (Join-Path $installDir "PeekPin.exe") -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name UninstallString -Value $uninstallCommand -PropertyType String -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name NoModify -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name NoRepair -Value 1 -PropertyType DWord -Force | Out-Null
New-ItemProperty -Path $uninstallKey -Name EstimatedSize -Value $sizeKb -PropertyType DWord -Force | Out-Null

if (-not $Silent) {
    Write-Host "PeekPin installed to $installDir"
}

Start-Process (Join-Path $installDir "PeekPin.exe")
