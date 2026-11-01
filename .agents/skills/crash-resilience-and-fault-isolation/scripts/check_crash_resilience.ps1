[CmdletBinding()]
param(
    [string]$TargetFolder = "src\OmniForge.App"
)

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$ScanPath = Join-Path $RepoRoot $TargetFolder

Write-Host "[Crash Resilience Scanner] Auditing $ScanPath for dangerous async void patterns..." -ForegroundColor Cyan

$Files = Get-ChildItem -Path $ScanPath -Recurse -Filter "*.cs"
$Violations = 0

foreach ($File in $Files) {
    $Content = Get-Content -Path $File.FullName -Raw
    if ($Content -match "async\s+void\s+(?!App_|Main_|\w+_UnhandledException)") {
        Write-Warning "Potentially fatal 'async void' detected in: $($File.FullName)"
        $Violations++
    }
}

if ($Violations -eq 0) {
    Write-Host "[PASSED] Zero forbidden async void methods found across $TargetFolder." -ForegroundColor Green
} else {
    Write-Error "Found $Violations dangerous async void declarations. Replace with async Task or [RelayCommand] + ToolViewModelBase.RunSafeAsync."
}

