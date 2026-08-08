$ErrorActionPreference = "Stop"

$sourcePath = Join-Path $PSScriptRoot "..\MainForm.cs"
$source = Get-Content -Raw $sourcePath

$unsafeSettings = @(
    "--renderer-process-limit=1"
    "--js-flags=--max-old-space-size=512"
    "MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low"
)

$found = $unsafeSettings | Where-Object { $source.Contains($_) }
if ($found.Count -gt 0) {
    throw "WebView uses unsafe background-memory settings: $($found -join ', ')"
}

Write-Host "PASS: WebView lifecycle configuration does not starve the renderer."
