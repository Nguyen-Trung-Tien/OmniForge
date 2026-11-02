[CmdletBinding()]
param(
    [string]$TargetFolder = "src\OmniForge.App"
)

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\..\.."
$ScanPath = Join-Path $RepoRoot $TargetFolder

Write-Host "[Crash Resilience Scanner] Auditing $ScanPath for dangerous async void patterns..." -ForegroundColor Cyan

$Files = Get-ChildItem -Path $ScanPath -Recurse -File -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }
$Violations = 0

foreach ($File in $Files) {
    $Content = Get-Content -Path $File.FullName -Raw
    if ($File.Name -notlike "*.xaml.cs" -and $File.Name -ne "MainWindow.cs") {
        if ($Content -match "async\s+(?:partial\s+)?void\s+") {
            Write-Warning "Potentially fatal 'async void' detected in ViewModel/Service: $($File.FullName)"
            $Violations++
        }
    } elseif ($Content -match "async\s+void\s+(?!App_|Main_|\w+_UnhandledException)" -and $Content -notmatch "try\s*\{") {
        Write-Warning "Unprotected 'async void' event handler without try/catch in: $($File.FullName)"
        $Violations++
    }
}

if ($Violations -eq 0) {
    Write-Host "[PASSED] Zero forbidden async void methods found across $TargetFolder." -ForegroundColor Green
} else {
    Write-Error "Found $Violations dangerous async void declarations. Replace with async Task or [RelayCommand] + ToolViewModelBase.RunSafeAsync."
}

