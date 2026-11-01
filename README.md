# OmniForge Utility Suite

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows App SDK](https://img.shields.io/badge/Windows%20App%20SDK-WinUI%203-0078D4?logo=windows)](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)
[![Fluent Design](https://img.shields.io/badge/Design-Fluent%20MicaAlt-0078D4)](https://learn.microsoft.com/en-us/windows/apps/design/style/mica)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**OmniForge** is a modular, high-performance developer and system utility suite for Windows 11, crafted with **.NET 9**, **WinUI 3 (Windows App SDK)**, and **Clean Architecture**. It features an extensible plugin subsystem, Mica visual depth, async non-blocking operations, and AI agent automation skills.

---

## Architecture Overview

OmniForge enforces strict Clean Architecture boundaries with zero code-behind logic and compile-time data bindings:

```
[ OmniForge.App ] (WinUI 3 Presentation, Shell, IoC Host)
       │
       ▼
[ OmniForge.Services ] (Navigation, File I/O, Local Settings, Tool Registry)
       │
       ▼
[ OmniForge.Core ] (Contracts: IToolModule, INavigationService, Result<T>)
```

- **Plugin-First Design**: Utilities are self-contained modules implementing `IToolModule` and auto-register with the shell.
- **MVVM Source Generators**: Powered by `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`).
- **Decoupled Navigation**: ViewModels trigger navigation via `INavigationService` without depending on UI `Frame` controls.
- **Persistent Local Settings**: JSON configuration stored securely under `%LocalAppData%\OmniForge\settings.json`.
- **Fault-Isolated Async I/O**: Operations return functional `Result<T>` structures with full cancellation token support.

For comprehensive architectural specifications, review [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

---

## Repository Structure

```
OmniForge/
├── .agents/                    # Agent governance, rules, and AI skills
│   ├── AGENTS.md               # Master architectural protocol
│   ├── rules/                  # Specific engineering rules (WinUI 3, Clean Code, Async I/O)
│   └── skills/                 # Antigravity AI skills (Module Generator, Batch I/O, Win2D)
├── docs/                       # System architecture blueprint and product roadmap
├── plans/                      # Granular phase execution plans
├── src/
│   ├── OmniForge.Core/         # Domain models, contracts, functional Result
│   ├── OmniForge.Services/     # Decoupled navigation, settings, and I/O implementations
│   └── OmniForge.App/          # WinUI 3 presentation shell, modules, and composition root
├── Directory.Build.props       # Global compiler flags & nullability enforcement
├── OmniForge.sln               # Primary solution file
└── CHANGELOG.md                # Project release history
```

---

## Build Prerequisites

To build and run OmniForge on Windows 11 / Windows 10 (version 1809 / build 17763 or newer):

1. **.NET 9 SDK**: Version 9.0.100 or later ([Download .NET 9](https://dotnet.microsoft.com/download/dotnet/9.0)).
2. **Visual Studio 2022** (v17.10+) or **VS Code** with C# Dev Kit:
   - Workload: *.NET Desktop Development*
   - Component: *Windows App SDK C# Templates*
3. **Windows 10/11 Developer Mode** enabled in Windows Settings.

---

## Unified CLI Workflow (`.\forge` — Zero Directory Hopping)

Run all build, execution, test, scaffolding, diagnostic, and learning commands directly from the solution root using `.\forge`:

```powershell
# 1. Full Verification Pipeline (Static Arch Guardrails + Build + xUnit & NetArchTest)
.\forge verify

# 2. Build or Launch the WinUI 3 Application
.\forge build
.\forge run

# 3. Run Automated Tests (All or Filtered)
.\forge test
.\forge test -Filter "ResultTests"

# 4. One-Command Module & Unit Test Scaffolding
.\forge new-module -Name "HashValidator" -DisplayName "Hash Validator" -Category "Security"
.\forge new-test -Name "HashValidator" -SubFolder "Modules"

# 5. Record or Inspect Institutional Agent Memory (.agents/MEMORY.md)
.\forge learn "WinUI 3 Frame.Navigate requires parameterless Page constructors" -Category "WinUI3"
.\forge learn

# 6. Inspect Runtime JSONL Logs & Environment Health
.\forge logs -Tail 30
.\forge doctor
.\forge clean
.\forge publish -Platform x64
```

---

## Agent & Automation Skills (`.agents/skills/`)

OmniForge includes 15 specialized AI Agent skills in `.agents/skills/`:
- **`cli-task-runner`**: Single-command root terminal workflow (`.\forge`) avoiding subdirectory traversal.
- **`security-and-input-hardening`**: Mandatory security workflow (`SecurityGuard`, Path Traversal defense, ReDoS timeout, XML/JSON DoS limits).
- **`performance-and-memory-optimization`**: 60+ FPS UI and zero-allocation workflow (`BulkObservableCollection<T>`, `ArrayPool<byte>`, `Span<T>`).
- **`crash-resilience-and-fault-isolation`**: WinUI 3 anti-crash workflow (zero `async void`, thread marshaling, `COMException` shielding).
- **`reusable-code-and-ui-architecture`**: Master DRY inventory of shared C# helpers, services, and reusable XAML controls.
- **`token-efficient-coding`**: High-density C# 13 and XAML patterns cutting token usage by 40–65%.
- **`winui3-module-generator`**: Zero-touch `IToolModule` scaffolding (`Service`, `ViewModel`, `Page.xaml`, `Module`, and `Tests`).
- **`unit-test-scaffolder`**: Rapid deterministic `xUnit` and `NetArchTest` generation without heavy mocking libraries.
- **`batch-io-processor`**: Chunked, cancellable batch file processing with `IAsyncEnumerable<T>` and `BatchProgressReport`.
- **`crypto-hash-engine`**: Zero-allocation async file hashing (`SHA-256`, `SHA-512`, `MD5`) using `ArrayPool<byte>.Shared`.
- **`text-payload-inspector`**: Streamed `Utf8JsonWriter` JSON/XML formatting, Base64/JWT inspection, and line diffing.
- **`regex-bulk-renamer`**: ReDoS-safe Regex bulk file renaming, token interpolation, collision preview, and Undo Journal rollback.
- **`fluent-ui-design-system`**: WinUI 3 Fluent theme brushes, typography, and Segoe Fluent Icons cheat-sheet.
- **`win2d-canvas-renderer`**: High-FPS Win2D `CanvasControl` rendering and GPU resource lifecycle management.
- **`msix-release-packager`**: Trimming-safe WinUI 3 MSIX and self-contained portable release packaging.

