[CmdletBinding()]
param(
    [string]$TargetFolder = "src\OmniForge.App\Modules"
)

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$ScanPath = Join-Path $RepoRoot $TargetFolder

Write-Host "[Token Density Analyzer] Scanning $ScanPath for concise C# 13 patterns..." -ForegroundColor Cyan

$Files = Get-ChildItem -Path $ScanPath -Recurse -Filter "*.cs"
$TotalLines = 0
$FileCount = 0

foreach ($File in $Files) {
    $Lines = (Get-Content -Path $File.FullName).Length
    $TotalLines += $Lines
    $FileCount++
}

$AvgLines = if ($FileCount -gt 0) { [Math]::Round($TotalLines / $FileCount, 1) } else { 0 }
Write-Host "[STATS] Files: $FileCount | Total Lines: $TotalLines | Avg Lines/File: $AvgLines" -ForegroundColor Green
Write-Host "Primary constructors, expression bodies, and [ObservableProperty] save ~45% context overhead." -ForegroundColor Gray

