[CmdletBinding()]
param(
    [string]$TargetFolder = "src\OmniForge.App\Modules"
)

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$ScanPath = Join-Path $RepoRoot $TargetFolder

Write-Host "[Architecture Reuse Audit] Auditing $ScanPath for standard base classes..." -ForegroundColor Cyan

$ViewModels = Get-ChildItem -Path $ScanPath -Recurse -Filter "*ViewModel.cs"
$Violations = 0

foreach ($Vm in $ViewModels) {
    $Content = Get-Content -Path $Vm.FullName -Raw
    if ($Content -notmatch ":\s*ToolViewModelBase") {
        Write-Warning "$($Vm.Name) does not inherit from ToolViewModelBase!"
        $Violations++
    }
}

if ($Violations -eq 0) {
    Write-Host "[PASSED] 100% of module ViewModels inherit from ToolViewModelBase." -ForegroundColor Green
} else {
    Write-Warning "Found $Violations non-standard ViewModels."
}

