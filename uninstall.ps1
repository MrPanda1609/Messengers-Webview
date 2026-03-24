
#Requires -Version 5.1
$appName = "Messenger"
$installDir = Join-Path $env:LOCALAPPDATA "MessengerLite"
$dataDir = Join-Path $env:LOCALAPPDATA "MessengerWrapper"

Write-Host ""
Write-Host "  Messenger Lite - Uninstaller" -ForegroundColor Cyan
Write-Host ""

if (Get-Process -Name "Messenger" -ErrorAction SilentlyContinue) {
    Write-Host "  Closing Messenger..." -ForegroundColor Yellow
    Stop-Process -Name "Messenger" -Force
    Start-Sleep -Seconds 1
}

if (Test-Path $installDir) {
    Remove-Item $installDir -Recurse -Force
    Write-Host "  Removed app files." -ForegroundColor Gray
}

# Remove shortcuts
$desktopLink = Join-Path ([Environment]::GetFolderPath("Desktop")) "$appName.lnk"
$startMenuLink = Join-Path ([Environment]::GetFolderPath("Programs")) "$appName.lnk"
if (Test-Path $desktopLink) { Remove-Item $desktopLink -Force }
if (Test-Path $startMenuLink) { Remove-Item $startMenuLink -Force }
Write-Host "  Removed shortcuts." -ForegroundColor Gray

$answer = Read-Host "  Delete login data & chat cache? (y/N)"
if ($answer -eq "y") {
    if (Test-Path $dataDir) { Remove-Item $dataDir -Recurse -Force }
    Write-Host "  Removed cached data." -ForegroundColor Gray
}

Write-Host ""
Write-Host "  Uninstalled successfully." -ForegroundColor Green
Write-Host ""
