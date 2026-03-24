
#Requires -Version 5.1
<#
.SYNOPSIS
    One-command installer for Messenger Lite Desktop
.DESCRIPTION
    Downloads the latest release from GitHub, installs to AppData, and creates shortcuts.
.USAGE
    irm https://raw.githubusercontent.com/MrPanda1609/Messengers-Webview/main/install.ps1 | iex
#>

$ErrorActionPreference = "Stop"
$repo = "MrPanda1609/Messengers-Webview"
$appName = "Messenger"
$installDir = Join-Path $env:LOCALAPPDATA "MessengerLite"

Write-Host ""
Write-Host "  Messenger Lite Desktop - Installer" -ForegroundColor Cyan
Write-Host "  ===================================" -ForegroundColor Cyan
Write-Host ""

# Get latest release
Write-Host "  [1/4] Fetching latest release..." -ForegroundColor Yellow
$release = Invoke-RestMethod "https://api.github.com/repos/$repo/releases/latest"
$asset = $release.assets | Where-Object { $_.name -like "*.zip" } | Select-Object -First 1

if (-not $asset) {
    Write-Host "  ERROR: No release found." -ForegroundColor Red
    exit 1
}

Write-Host "        Version: $($release.tag_name)" -ForegroundColor Gray

# Download
$zipPath = Join-Path $env:TEMP "messenger-lite.zip"
Write-Host "  [2/4] Downloading ($([math]::Round($asset.size / 1MB, 1)) MB)..." -ForegroundColor Yellow
Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -UseBasicParsing

# Extract
Write-Host "  [3/4] Installing to $installDir ..." -ForegroundColor Yellow
if (Get-Process -Name "Messenger" -ErrorAction SilentlyContinue) {
    Stop-Process -Name "Messenger" -Force
    Start-Sleep -Seconds 1
}
if (Test-Path $installDir) { Remove-Item $installDir -Recurse -Force }
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Expand-Archive -Path $zipPath -DestinationPath $installDir -Force
Remove-Item $zipPath -Force

# Create shortcuts
Write-Host "  [4/4] Creating shortcuts..." -ForegroundColor Yellow
$exePath = Join-Path $installDir "Messenger.exe"
$shell = New-Object -ComObject WScript.Shell

# Desktop shortcut
$desktopLink = Join-Path ([Environment]::GetFolderPath("Desktop")) "$appName.lnk"
$shortcut = $shell.CreateShortcut($desktopLink)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "Messenger Lite Desktop"
$shortcut.Save()

# Start Menu shortcut
$startMenu = Join-Path ([Environment]::GetFolderPath("Programs")) "$appName.lnk"
$shortcut = $shell.CreateShortcut($startMenu)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = "Messenger Lite Desktop"
$shortcut.Save()

Write-Host ""
Write-Host "  Installed successfully!" -ForegroundColor Green
Write-Host "  Shortcuts created on Desktop and Start Menu." -ForegroundColor Gray
Write-Host ""
Write-Host "  Launching Messenger..." -ForegroundColor Cyan
Start-Process $exePath
