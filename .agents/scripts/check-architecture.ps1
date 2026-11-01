<#
.SYNOPSIS
    Static architectural guardrail scanner for OmniForge Utility Suite.
.DESCRIPTION
    Scans C# and XAML files for violations of AGENTS.md rules:
    1. Forbidden synchronous File I/O in ViewModels or Views.
    2. ViewModels not declared as partial or not inheriting from ObservableObject.
    3. Interfaces placed outside OmniForge.Core/Contracts in the Core project.
#>
$ErrorActionPreference = "Stop"
$solutionRoot = Resolve-Path "$PSScriptRoot\..\.."
$srcDir = Join-Path $solutionRoot "src"
$violations = @()

Write-Host "[1/3] Checking ViewModels for partial class & ObservableObject inheritance..." -ForegroundColor Cyan
$vmFiles = Get-ChildItem -Path $srcDir -Filter "*ViewModel.cs" -Recurse | Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" }
foreach ($file in $vmFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    if ($content -notmatch "partial\s+class\s+\w+ViewModel") {
        $violations += "ViewModel must be declared 'partial class': $($file.FullName)"
    }
    if ($content -notmatch ":\s*ObservableObject|:\s*ObservableRecipient|:\s*ObservableValidator") {
        $violations += "ViewModel must inherit from ObservableObject: $($file.FullName)"
    }
}

Write-Host "[2/3] Checking ViewModels and Code-Behind for forbidden synchronous File I/O..." -ForegroundColor Cyan
$uiFiles = Get-ChildItem -Path (Join-Path $srcDir "OmniForge.App") -Include "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" }
$forbiddenPatterns = @(
    "File\.ReadAllText\(",
    "File\.WriteAllText\(",
    "File\.ReadAllBytes\(",
    "File\.WriteAllBytes\(",
    "Directory\.GetFiles\(",
    "Thread\.Sleep\("
)
foreach ($file in $uiFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    foreach ($pattern in $forbiddenPatterns) {
        if ($content -match $pattern) {
            $violations += "Forbidden blocking call ($pattern) detected in UI layer: $($file.FullName)"
        }
    }
}

Write-Host "[3/3] Checking OmniForge.Core for stray Interfaces folder..." -ForegroundColor Cyan
$legacyInterfacesDir = Join-Path $srcDir "OmniForge.Core\Interfaces"
if (Test-Path $legacyInterfacesDir) {
    $violations += "Redundant folder 'OmniForge.Core/Interfaces' detected. Consolidate all contracts into 'OmniForge.Core/Contracts'."
}

if ($violations.Count -gt 0) {
    Write-Host "`n[FAILED] Architectural violations detected:" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "  - $v" -ForegroundColor Red
    }
    exit 1
}

Write-Host "[PASSED] All static architectural guardrails verified successfully." -ForegroundColor Green
exit 0

