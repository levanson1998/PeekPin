param(
    [string]$Source,
    [switch]$Silent
)

function Resolve-AppDirectory {
    param([string]$Requested)

    if ($Requested -and (Test-Path (Join-Path $Requested "PeekPin.exe"))) {
        return (Resolve-Path $Requested).Path
    }

    $candidates = @(
        (Join-Path $PSScriptRoot "app"),
        (Join-Path $PSScriptRoot "..\artifacts\publish"),
        (Join-Path $PSScriptRoot "..\dist\PeekPin-1.0.0-win-x64\app")
    )
    foreach ($candidate in $candidates) {
        if (Test-Path (Join-Path $candidate "PeekPin.exe")) {
            return (Resolve-Path $candidate).Path
        }
    }

    return (Join-Path $PSScriptRoot "app")
}

$ErrorActionPreference = "Stop"
$installDir = Join-Path $env:ProgramFiles "PeekPin"
$previousDir = Join-Path $env:LOCALAPPDATA "Programs\PeekPin"
$sourceDir = Resolve-AppDirectory $Source
$exe = Join-Path $sourceDir "PeekPin.exe"

if (-not (Test-Path $exe)) {
    throw "PeekPin.exe was not found. From the repo, run installer\build.ps1 first, then Setup.cmd."
}

Get-Process PeekPin -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item -Path (Join-Path $sourceDir "*") -Destination $installDir -Recurse -Force
Copy-Item -Path (Join-Path $PSScriptRoot "Uninstall.ps1") -Destination (Join-Path $installDir "Uninstall.ps1") -Force

$uninstallCmd = @"
@echo off
net session >nul 2>&1
if errorlevel 1 (
  powershell.exe -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall.ps1" %*
"@
Set-Content -Path (Join-Path $installDir "Uninstall.cmd") -Value $uninstallCmd -Encoding ASCII

$startMenu = Join-Path $env:ProgramData "Microsoft\Windows\Start Menu\Programs"
$shortcutPath = Join-Path $startMenu "PeekPin.lnk"
$oldShortcut = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\PeekPin.lnk"
if (Test-Path $oldShortcut) {
    Remove-Item $oldShortcut -Force
}
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $installDir "PeekPin.exe"
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "PeekPin"
$shortcut.Save()

$oldUninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\PeekPin"
if (Test-Path $oldUninstallKey) {
    Remove-Item $oldUninstallKey -Recurse -Force
}
if ((Test-Path $previousDir) -and ($previousDir -ne $installDir)) {
    Remove-Item $previousDir -Recurse -Force -ErrorAction SilentlyContinue
}

$uninstallKey = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\PeekPin"
New-Item -Path $uninstallKey -Force | Out-Null
$sizeKb = [int]((Get-ChildItem $installDir -Recurse -File | Measure-Object Length -Sum).Sum / 1KB)
$uninstallCommand = "`"$installDir\Uninstall.cmd`""
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
