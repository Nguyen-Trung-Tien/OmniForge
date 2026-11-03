<#
.SYNOPSIS
    OmniForge Unified CLI Task Runner (Zero-Directory-Hopping Entrypoint).
.DESCRIPTION
    Provides single-command execution from the solution root for building, running, testing,
    architectural verification, module/test scaffolding, log inspection, and agent learning.
.EXAMPLE
    .\forge.ps1 build
    .\forge.ps1 run
    .\forge.ps1 verify
    .\forge.ps1 test -Filter "ResultTests"
    .\forge.ps1 new-module -Name "HashValidator" -DisplayName "Hash Validator"
    .\forge.ps1 new-test -Name "HashValidator"
    .\forge.ps1 learn "WinUI 3 Frame.Navigate requires parameterless Page constructors"
    .\forge.ps1 logs -Tail 25
    .\forge.ps1 doctor
#>
param(
    [Parameter(Position = 0)]
    [ValidateSet("build", "run", "test", "verify", "arch", "new-module", "new-test", "new-skill", "distill-issue", "learn", "logs", "doctor", "clean", "publish", "package", "help")]
    [string]$Command = "help",

    [Parameter(Position = 1)]
    [string]$Name = "",

    [string]$DisplayName = "",
    [string]$Description = "",
    [string]$Symptom = "",
    [string]$RootCause = "",
    [string]$Resolution = "",
    [string]$Category = "Developer Tools",
    [string]$IconGlyph = "\uE8B7",
    [string]$SubFolder = "Modules",
    [string]$Filter = "",
    [ValidateSet("Debug", "Release")]
    [string]$Config = "Debug",
    [ValidateSet("x64", "ARM64", "x86")]
    [string]$Platform = "x64",
    [int]$Tail = 30,
    [switch]$Clear
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$SlnPath = Join-Path $Root "OmniForge.sln"
$AppProj = Join-Path $Root "src\OmniForge.App\OmniForge.App.csproj"
$TestProj = Join-Path $Root "tests\OmniForge.Tests\OmniForge.Tests.csproj"
$MemoryFile = Join-Path $Root ".agents\MEMORY.md"
$LogDir = Join-Path $env:LOCALAPPDATA "OmniForge\logs"

function Write-Header([string]$Title) {
    Write-Host "`n==========================================================" -ForegroundColor Cyan
    Write-Host "  OmniForge CLI :: $Title" -ForegroundColor Cyan
    Write-Host "==========================================================" -ForegroundColor Cyan
}

switch ($Command) {
    "build" {
        Write-Header "Build Solution ($Config | $Platform)"
        Get-Process -Name "OmniForge.App" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        dotnet build $SlnPath -c $Config -p:Platform=$Platform --nologo
        exit $LASTEXITCODE
    }

    "run" {
        Write-Header "Run OmniForge.App ($Config | $Platform)"
        Get-Process -Name "OmniForge.App" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        dotnet run --project $AppProj -c $Config -p:Platform=$Platform
        exit $LASTEXITCODE
    }

    "test" {
        Write-Header "Run Automated xUnit & NetArchTest Suite ($Config)"
        if ([string]::IsNullOrWhiteSpace($Filter)) {
            dotnet test $TestProj -c $Config -p:Platform=$Platform --nologo --verbosity minimal
        } else {
            dotnet test $TestProj -c $Config -p:Platform=$Platform --filter $Filter --nologo --verbosity normal
        }
        exit $LASTEXITCODE
    }

    "arch" {
        Write-Header "Fast Static Architecture Guardrails"
        & "$Root\.agents\scripts\check-architecture.ps1"
        exit $LASTEXITCODE
    }

    "verify" {
        & "$Root\.agents\scripts\verify-all.ps1"
        exit $LASTEXITCODE
    }

    "new-module" {
        if ([string]::IsNullOrWhiteSpace($Name)) {
            Write-Host "[ERROR] Specify module name: .\forge.ps1 new-module <ModuleName> [-DisplayName '...']" -ForegroundColor Red
            exit 1
        }
        if ([string]::IsNullOrWhiteSpace($DisplayName)) {
            $DisplayName = ($Name -creplace '([a-z])([A-Z])', '$1 $2')
        }
        Write-Header "Scaffold Module: $Name ($DisplayName)"
        & "$Root\.agents\skills\winui3-module-generator\scripts\scaffold_module.ps1" `
            -ModuleName $Name `
            -DisplayName $DisplayName `
            -Category $Category `
            -IconGlyph $IconGlyph `
            -IncludeUnitTest
        exit $LASTEXITCODE
    }

    "new-test" {
        if ([string]::IsNullOrWhiteSpace($Name)) {
            Write-Host "[ERROR] Specify target name: .\forge.ps1 new-test <TargetName> [-SubFolder 'Services']" -ForegroundColor Red
            exit 1
        }
        Write-Header "Scaffold Unit Test: ${Name}Tests"
        & "$Root\.agents\skills\unit-test-scaffolder\scripts\scaffold_test.ps1" `
            -TargetName $Name `
            -SubFolder $SubFolder
        exit $LASTEXITCODE
    }

    "new-skill" {
        if ([string]::IsNullOrWhiteSpace($Name)) {
            Write-Host "[ERROR] Specify skill name: .\forge.ps1 new-skill <SkillName> [-Description '...']" -ForegroundColor Red
            exit 1
        }
        $SkillSlug = $Name.ToLower().Replace(" ", "-").Replace("_", "-")
        $SkillDir = Join-Path $Root ".agents\skills\$SkillSlug"

        Write-Header "Scaffold Agent Skill: $SkillSlug"
        Write-Host "Documentation Reference: https://antigravity.google/docs/skills/" -ForegroundColor DarkGray
        Write-Host "Note: Antigravity defaults to .agents/skills, maintaining backward compatibility for .agent/skills.`n" -ForegroundColor DarkGray

        if (Test-Path $SkillDir) {
            Write-Host "[ERROR] Skill directory already exists: $SkillDir" -ForegroundColor Red
            exit 1
        }

        # Create standard 4-part directory structure
        New-Item -ItemType Directory -Path (Join-Path $SkillDir "scripts") -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $SkillDir "examples") -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $SkillDir "resources") -Force | Out-Null

        $DescText = if ([string]::IsNullOrWhiteSpace($Description)) {
            "Provides specialized workflow and instructions for $SkillSlug."
        } else {
            $Description
        }

        $SkillMdContent = @"
---
name: $SkillSlug
description: $DescText
---

# $SkillSlug Skill

## Overview
$DescText

## Directory Structure
- scripts/: Executable scripts and automation helpers
- examples/: Reference implementations and code samples
- resources/: Templates, schemas, or data files

## Procedures & Guidelines
1. Step 1...
2. Step 2...

## Documentation Reference
See https://antigravity.google/docs/skills/ for official Antigravity skill architecture guidelines.
"@

        Set-Content -Path (Join-Path $SkillDir "SKILL.md") -Value $SkillMdContent -Encoding UTF8
        Write-Host "[SUCCESS] Scaffolding complete for skill: $SkillSlug" -ForegroundColor Green
        Write-Host "  Location : $SkillDir" -ForegroundColor Cyan
        Write-Host "  Structure: SKILL.md, scripts/, examples/, resources/" -ForegroundColor Cyan
        Write-Host "`n[MANDATORY NEXT STEPS]" -ForegroundColor Yellow
        Write-Host "  1. Review instructions at https://antigravity.google/docs/skills/" -ForegroundColor Yellow
        Write-Host "  2. Update .agents/AGENTS.md and relevant usage locations" -ForegroundColor Yellow
        exit 0
    }

    "distill-issue" {
        if ([string]::IsNullOrWhiteSpace($Name)) {
            Write-Host "[ERROR] Specify issue name: .\forge.ps1 distill-issue <IssueName> [-Description '...'] [-Symptom '...'] [-RootCause '...'] [-Resolution '...']" -ForegroundColor Red
            exit 1
        }
        & "$Root\.agents\skills\issue-skill-distiller\scripts\distill_issue_skill.ps1" `
            -Name $Name `
            -Description $Description `
            -Symptom $Symptom `
            -RootCause $RootCause `
            -Resolution $Resolution `
            -Category $Category
        exit $LASTEXITCODE
    }

    "learn" {
        Write-Header "Agent Institutional Memory & Learned Patterns (.agents/MEMORY.md)"
        if ([string]::IsNullOrWhiteSpace($Name)) {
            if (Test-Path $MemoryFile) {
                Get-Content -Path $MemoryFile | Write-Host
            } else {
                Write-Host "No MEMORY.md found yet." -ForegroundColor Yellow
            }
            Write-Host "`n[TIP] To record a new insight: .\forge.ps1 learn `"Your architectural lesson here`"" -ForegroundColor Green
            exit 0
        }

        $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm")
        $entry = "- **[$timestamp]** ($Category): $Name"
        Add-Content -Path $MemoryFile -Value $entry -Encoding UTF8
        Write-Host "[LEARNED] Appended to .agents/MEMORY.md:" -ForegroundColor Green
        Write-Host "  $entry" -ForegroundColor Cyan
        exit 0
    }

    "logs" {
        Write-Header "Structured JSONL Diagnostic Logs"
        if (-not (Test-Path $LogDir)) {
            Write-Host "Log directory does not exist yet: $LogDir" -ForegroundColor Yellow
            exit 0
        }
        if ($Clear) {
            Remove-Item -Path (Join-Path $LogDir "*.jsonl") -Force -ErrorAction SilentlyContinue
            Write-Host "[CLEARED] Removed all log files in $LogDir" -ForegroundColor Green
            exit 0
        }
        $latestLog = Get-ChildItem -Path $LogDir -Filter "*.jsonl" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($null -eq $latestLog) {
            Write-Host "No .jsonl log files found in $LogDir" -ForegroundColor Yellow
            exit 0
        }
        Write-Host "Showing last $Tail entries from $($latestLog.FullName):`n" -ForegroundColor Yellow
        Get-Content -Path $latestLog.FullName -Tail $Tail
        exit 0
    }

    "doctor" {
        Write-Header "OmniForge Environment & Architecture Doctor"
        $sdkVersion = dotnet --version
        Write-Host "[OK] .NET SDK Version       : $sdkVersion" -ForegroundColor Green
        Write-Host "[OK] Solution Path          : $SlnPath" -ForegroundColor Green

        $skills = Get-ChildItem -Path (Join-Path $Root ".agents\skills") -Directory
        Write-Host "[OK] Installed Agent Skills : $($skills.Count) ($($skills.Name -join ', '))" -ForegroundColor Green

        $rules = Get-ChildItem -Path (Join-Path $Root ".agents\rules") -Filter "*.md"
        Write-Host "[OK] Governance Rule Files  : $($rules.Count) ($($rules.Name -join ', '))" -ForegroundColor Green

        & "$Root\.agents\scripts\check-architecture.ps1"
        exit $LASTEXITCODE
    }

    "clean" {
        Write-Header "Clean Build Artifacts (bin / obj)"
        dotnet clean $SlnPath --nologo -v minimal
        Get-ChildItem -Path $Root -Include "bin", "obj" -Directory -Recurse | Where-Object { $_.FullName -notmatch "\\.git\\" } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "[CLEANED] All bin/ and obj/ directories removed." -ForegroundColor Green
        exit 0
    }

    "publish" {
        Write-Header "Publish Self-Contained Release (win-$Platform)"
        $rid = "win-$($Platform.ToLowerInvariant())"
        $outputDir = Join-Path $Root "release\publish\$rid"
        dotnet publish $AppProj -c Release -r $rid -p:Platform=$Platform --self-contained true -o $outputDir --nologo
        exit $LASTEXITCODE
    }

    "package" {
        Write-Header "Package Application via Inno Setup (win-$Platform)"
        $rid = "win-$($Platform.ToLowerInvariant())"
        $outputDir = Join-Path $Root "release\publish\$rid"
        Write-Host "[1/2] Publishing self-contained release to $outputDir..." -ForegroundColor Cyan
        dotnet publish $AppProj -c Release -r $rid -p:Platform=$Platform --self-contained true -o $outputDir --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[ERROR] dotnet publish failed." -ForegroundColor Red
            exit $LASTEXITCODE
        }

        Write-Host "[2/2] Compiling Inno Setup installer..." -ForegroundColor Cyan
        $issPath = Join-Path $Root "installer\OmniForge.iss"
        if (-not (Test-Path $issPath)) {
            Write-Host "[ERROR] Installer script not found: $issPath" -ForegroundColor Red
            exit 1
        }

        $isccCmd = Get-Command iscc -ErrorAction SilentlyContinue
        $isccFromPath = if ($isccCmd) { $isccCmd.Source } else { $null }
        $isccCandidates = @(
            $isccFromPath,
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\iscc.exe",
            "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe",
            "$env:ProgramFiles\Inno Setup 6\iscc.exe"
        ) | Where-Object { $_ -and (Test-Path $_) }

        $isccPath = $isccCandidates | Select-Object -First 1
        if (-not $isccPath) {
            Write-Host "[ERROR] Inno Setup compiler (iscc.exe) not found on system." -ForegroundColor Red
            exit 1
        }

        & $isccPath $issPath
        if ($LASTEXITCODE -eq 0) {
            $releaseInstallerDir = Join-Path $Root "release\installer"
            if (-not (Test-Path $releaseInstallerDir)) {
                New-Item -Path $releaseInstallerDir -ItemType Directory -Force | Out-Null
            }

            # Generate SHA-256 Checksums in release directory
            $checksumFile = Join-Path $Root "release\checksums.txt"
            $generatedExe = Get-ChildItem -Path $releaseInstallerDir -Filter "*.exe" | Select-Object -First 1
            if ($generatedExe) {
                $hash = (Get-FileHash -Path $generatedExe.FullName -Algorithm SHA256).Hash
                "$hash  $($generatedExe.Name)" | Out-File -FilePath $checksumFile -Encoding utf8
            }

            Write-Host "[SUCCESS] Production Installer deployed directly to release\installer\" -ForegroundColor Green
            Write-Host "[SUCCESS] SHA-256 Checksum recorded in release\checksums.txt" -ForegroundColor Green
        }
        exit $LASTEXITCODE
    }

    default {
        Write-Header "Unified Command Reference (Run from Root)"
        Write-Host "  .\forge build                Build solution (Debug | x64, TreatWarningsAsErrors)"
        Write-Host "  .\forge run                  Build and launch OmniForge WinUI 3 shell"
        Write-Host "  .\forge test [-Filter X]     Run xUnit + NetArchTest suite without directory hopping"
        Write-Host "  .\forge verify               Full pipeline: Static Arch Check + Build + Unit Tests"
        Write-Host "  .\forge arch                 Fast (<1s) static architecture guardrail scan"
        Write-Host "  .\forge new-module <Name>    Scaffold Module + ViewModel + XAML + Service + Test"
        Write-Host "  .\forge new-test <Name>      Scaffold deterministic xUnit test class"
        Write-Host "  .\forge new-skill <Name>     Scaffold standard 4-part AI Agent Skill (.agents/skills)"
        Write-Host "  .\forge distill-issue <Name> Distill newly resolved issue into a standardized skill"
        Write-Host "  .\forge learn `"<Insight>`"    Persist learned architectural/debugging insight to .agents/MEMORY.md"
        Write-Host "  .\forge logs [-Tail 30]      Inspect or -Clear latest structured JSONL runtime logs"
        Write-Host "  .\forge doctor               Verify .NET SDK, skills inventory, and static architecture"
        Write-Host "  .\forge clean                Purge all bin/ and obj/ artifacts"
        Write-Host "  .\forge publish              Publish self-contained Release build"
        Write-Host "  .\forge package              Build, package, and deploy installer to release\installer"
        exit 0
    }
}

