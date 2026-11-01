# OmniForge Testing & Architecture Verification Protocol

This document defines mandatory testing standards, architecture verification rules, and automated feedback loops for AI Agents and engineers working on the **OmniForge Utility Suite**.

---

## 1. Automated Verification Loop for AI Agents

Every AI Agent task that modifies source code in `src/` MUST execute the unified verification script before reporting completion:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .agents/scripts/verify-all.ps1
```

A task is **incomplete** if any of the following fail:
1. Static Architectural Guardrails (`check-architecture.ps1`).
2. Solution Compilation with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
3. Unit, Integration, and NetArchTest Suites (`OmniForge.Tests`).

---

## 2. Three-Tier Testing Strategy

### 2.1 Domain & Functional Core Tests (`tests/OmniForge.Tests/Core/`)
- Test `Result`, `Result<T>`, `Error`, and domain models in complete isolation.
- Zero mocking or file system dependencies.

### 2.2 Infrastructure & Service Integration Tests (`tests/OmniForge.Tests/Services/`)
- Test `FileSystemService`, `LocalSettingsService`, `ToolRegistryService`, and `FileLoggerProvider`.
- When testing disk I/O, always create an isolated temporary directory (`Path.Combine(Path.GetTempPath(), "OmniForgeTests_" + Guid.NewGuid().ToString("N"))`) and clean it up inside `IDisposable.Dispose()`.

### 2.3 Architecture Governance Tests (`tests/OmniForge.Tests/Architecture/`)
- Powered by `NetArchTest.Rules`.
- Automatically verifies:
  - `OmniForge.Core` has zero dependencies on `OmniForge.Services`, `OmniForge.App`, or `Microsoft.UI.Xaml`.
  - `OmniForge.Services` has zero dependencies on `OmniForge.App`.
  - All Core abstractions reside in `OmniForge.Core.Contracts` and begin with `I`.

---

## 3. Test Naming & Assertion Standards

1. Follow the **`MethodOrSubject_Scenario_ExpectedOutcome`** naming convention (e.g., `FileSystemService_ReadNonExistentFile_ShouldReturnFailureResultWithFileNotFoundCode`).
2. Structure tests with clear **Arrange / Act / Assert** blocks.
3. Never use `Thread.Sleep` in tests; use `CancellationToken` and deterministic async waits.

