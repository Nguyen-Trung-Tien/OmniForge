<#
.SYNOPSIS
    Single-command verification pipeline for AI Agents and CI/CD.
.DESCRIPTION
    Executes static architectural checks, builds the entire solution with WarningsAsErrors,
    and runs all Unit, Integration, and Architecture Governance tests.
#>
$ErrorActionPreference = "Stop"
$solutionRoot = Resolve-Path "$PSScriptRoot\..\.."

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   OmniForge Production & Agentic Verification Pipeline   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Step 1: Static Architecture Scan
Write-Host "`n>>> Step 1: Running Static Architecture Guardrails..." -ForegroundColor Yellow
& "$PSScriptRoot\check-architecture.ps1"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Step 2: Build Solution (x64 Debug)
Write-Host "`n>>> Step 2: Building Solution (TreatWarningsAsErrors=true)..." -ForegroundColor Yellow
dotnet build "$solutionRoot\OmniForge.sln" -c Debug -p:Platform=x64 --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAILED] Solution build failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

# Step 3: Run Automated Test Suite (Unit + Integration + NetArchTest)
Write-Host "`n>>> Step 3: Running xUnit & NetArchTest Verification Suite..." -ForegroundColor Yellow
dotnet test "$solutionRoot\tests\OmniForge.Tests\OmniForge.Tests.csproj" -c Debug --no-build --nologo --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "[FAILED] Test suite failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "   ALL CHECKS PASSED: Build, Unit Tests & Arch Rules OK   " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
exit 0

