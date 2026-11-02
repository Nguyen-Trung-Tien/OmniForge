<#
.SYNOPSIS
    Automated Issue-to-Skill Distiller for OmniForge AI Agents.
.DESCRIPTION
    Scaffolds a standardized 4-part Antigravity AI Agent Skill (.agents/skills/<name>)
    capturing problem symptoms, root causes, resolution runbooks, and regression checks.
.EXAMPLE
    .\distill_issue_skill.ps1 -Name "winui3-startup-navigation" -Description "Prevents startup navigation race conditions" -Symptom "NavigationService uninitialized"
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Name,

    [Parameter(Mandatory = $false)]
    [string]$Description = "",

    [Parameter(Mandatory = $false)]
    [string]$Symptom = "",

    [Parameter(Mandatory = $false)]
    [string]$RootCause = "",

    [Parameter(Mandatory = $false)]
    [string]$Resolution = "",

    [Parameter(Mandatory = $false)]
    [string]$Category = "Architecture"
)

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
$RepoRoot = Split-Path (Split-Path (Split-Path $ScriptDir -Parent) -Parent) -Parent

$SkillSlug = $Name.ToLower().Replace(" ", "-").Replace("_", "-").Trim("-")
$TargetSkillDir = Join-Path $RepoRoot ".agents\skills\$SkillSlug"

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "  OmniForge :: Issue-to-Skill Distiller" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Documentation Reference: https://antigravity.google/docs/skills/" -ForegroundColor DarkGray
Write-Host "Note: Antigravity defaults to .agents/skills, maintaining backward compatibility for .agent/skills.`n" -ForegroundColor DarkGray

if (Test-Path $TargetSkillDir) {
    Write-Host "[ERROR] Target skill directory already exists: $TargetSkillDir" -ForegroundColor Red
    exit 1
}

# 1. Create standard 4-part directory structure
New-Item -ItemType Directory -Path (Join-Path $TargetSkillDir "scripts") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $TargetSkillDir "examples") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $TargetSkillDir "resources") -Force | Out-Null

$FinalDesc = if ([string]::IsNullOrWhiteSpace($Description)) {
    "Diagnoses, resolves, and prevents $SkillSlug issues in OmniForge. Use when encountering related errors or modifying associated components."
} else {
    $Description
}

$FinalSymptom = if ([string]::IsNullOrWhiteSpace($Symptom)) {
    "- Observable error messages, unhandled exceptions, or visual glitches associated with $SkillSlug."
} else {
    "- $Symptom"
}

$FinalRootCause = if ([string]::IsNullOrWhiteSpace($RootCause)) {
    "- Architectural mismatch, unhandled lifecycle state, or race condition in component execution."
} else {
    "- $RootCause"
}

$FinalResolution = if ([string]::IsNullOrWhiteSpace($Resolution)) {
    "1. Isolate the failing component and reproduce with a targeted unit test.`n2. Apply defensive boundary guards and thread synchronization.`n3. Verify resolution with .\forge verify."
} else {
    $Resolution
}

# 2. Generate SKILL.md
$SkillMd = @"
---
name: $SkillSlug
description: $FinalDesc
---

# $SkillSlug Runbook & Resolution Skill

## 1. Problem Identification & Symptoms
$FinalSymptom

## 2. Root Cause Analysis
$FinalRootCause

## 3. Standardized Resolution Procedure
$FinalResolution

## 4. Prevention Guardrails & Verification
- Execute ``.\forge verify`` to ensure zero regressions across unit and architecture tests.
- Verify memory patterns in ``.agents/MEMORY.md``.

## 5. Architectural References
- Official Antigravity Skills: https://antigravity.google/docs/skills/
- OmniForge Agent Protocol: file:///.agents/AGENTS.md
"@

Set-Content -Path (Join-Path $TargetSkillDir "SKILL.md") -Value $SkillMd -Encoding UTF8

# 3. Create initial helper script stub
$VerifyScript = @"
<#
.SYNOPSIS
    Verification script for $SkillSlug.
#>
Write-Host "Verifying resolution for $SkillSlug..." -ForegroundColor Cyan
& "`$PSScriptRoot\..\..\..\forge.ps1" verify
exit `$LASTEXITCODE
"@
Set-Content -Path (Join-Path $TargetSkillDir "scripts\verify_${SkillSlug}.ps1") -Value $VerifyScript -Encoding UTF8

# 4. Record in MEMORY.md
$MemoryFile = Join-Path $RepoRoot ".agents\MEMORY.md"
if (Test-Path $MemoryFile) {
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm")
    $memoryEntry = "- **[$timestamp]** ($Category): Distilled skill `$SkillSlug` to resolve: $FinalDesc"
    Add-Content -Path $MemoryFile -Value $memoryEntry -Encoding UTF8
    Write-Host "[SUCCESS] Appended pattern to .agents/MEMORY.md" -ForegroundColor Green
}

Write-Host "[SUCCESS] Skill distilled successfully at: $TargetSkillDir" -ForegroundColor Green
Write-Host "  Structure: SKILL.md, scripts/, examples/, resources/" -ForegroundColor Cyan
Write-Host "`n[MANDATORY NEXT STEPS]" -ForegroundColor Yellow
Write-Host "  1. Review generated SKILL.md and refine problem-specific resolution steps" -ForegroundColor Yellow
Write-Host "  2. Ensure .agents/AGENTS.md reflects this skill" -ForegroundColor Yellow
Write-Host "  3. Update CHANGELOG.md under [Unreleased] -> ### Added" -ForegroundColor Yellow
exit 0

