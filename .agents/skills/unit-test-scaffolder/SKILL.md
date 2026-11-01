---
name: unit-test-scaffolder
description: Rapidly scaffolds deterministic xUnit and NetArchTest suites for OmniForge services, domain models, and modules with zero external mocking dependencies.
---

# Unit & Architecture Test Scaffolder Skill

Use this skill when adding unit tests, service integration tests, or architectural verification rules inside `tests/OmniForge.Tests/`.

---

## 1. Directory Structure & Assets

- Scaffolding Script: [scaffold_test.ps1](scripts/scaffold_test.ps1)
- Reference Test Suite: [DeterministicServiceTestExample.cs](examples/DeterministicServiceTestExample.cs)
- Test Class Template: [test-class-template.cs](resources/test-class-template.cs)

---

## 2. Core Testing Principles (Zero-Mock Lightweight Fakes)

1. **No Heavy Mocking Frameworks**: Prefer lightweight private `sealed record` or in-memory fake implementations of `OmniForge.Core.Contracts` interfaces. They compile faster, require zero reflection overhead, and use fewer tokens.
2. **Isolated Temp Directories**: For `FileSystemService` or `LocalSettingsService` tests, implement `IDisposable` and create a unique temp directory under `Path.GetTempPath()`.
3. **Fast Scaffolding Script**: Run `.agents/skills/unit-test-scaffolder/scripts/scaffold_test.ps1` to generate a test class template immediately.

---

## 3. Running Verification

Always run the unified verification script after adding or modifying tests:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/verify-all.ps1
```
