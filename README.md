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

## CLI Build & Run Commands

Execute the following commands from the solution root:

### Restore Dependencies
```powershell
dotnet restore OmniForge.sln
```

### Build Solution (Strict Zero-Warning Enforcement)
```powershell
dotnet build OmniForge.sln -c Debug
```

### Run the Application (Unpackaged / Packaged target)
```powershell
dotnet run --project src/OmniForge.App/OmniForge.App.csproj
```

### Publish Self-Contained Release for Windows x64
```powershell
dotnet publish src/OmniForge.App/OmniForge.App.csproj -c Release -r win-x64 --self-contained true
```

---

## Agent & Automation Skills

OmniForge includes pre-configured AI Agent skills in `.agents/skills/`:
- **`winui3-module-generator`**: Scaffolds a new utility plugin end-to-end (`Service`, `ViewModel`, `Page.xaml`, and `IToolModule`).
- **`batch-io-processor`**: Templates and guidelines for chunked, cancellable file operations.
- **`win2d-canvas-renderer`**: High-performance Win2D canvas rendering and device resource management.

