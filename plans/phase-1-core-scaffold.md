# Phase 1: Foundation, Governance & Architectural Core - Execution Checklist

This plan tracks the granular execution steps required to establish the foundational architecture, governance protocols, and core solution structure for the **OmniForge Utility Suite**.

---

## 1. Governance & Agent Protocols
- [x] Create `.agents/AGENTS.md` specifying Clean Architecture layers, zero code-behind, non-blocking async I/O, error isolation, and changelog rules.
- [x] Create `.agents/rules/winui3-rules.md` detailing `CommunityToolkit.Mvvm` source generator usage, UI thread synchronization (`DispatcherQueue`), and Fluent styling.
- [x] Create `.agents/rules/clean-code-rules.md` enforcing SOLID principles, strict nullability, exception wrapping, and modern C# conventions.
- [x] Create `.agents/rules/async-io-rules.md` establishing non-blocking streaming, `IAsyncEnumerable<T>`, cancellation tokens, and Win32 file exception handling.
- [x] Create `.agents/skills/winui3-module-generator/SKILL.md` with complete templates for scaffolding sub-tools.
- [x] Create `.agents/skills/batch-io-processor/SKILL.md` with channel-based chunked processing samples.
- [x] Create `.agents/skills/win2d-canvas-renderer/SKILL.md` for high-FPS Win2D canvas rendering and device resource management.

---

## 2. Documentation & Roadmapping
- [x] Create `docs/ARCHITECTURE.md` documenting Plugin System, Dynamic Navigation, IoC container, and State management.
- [x] Create `docs/ROADMAP.md` covering the 5-phase evolution to enterprise distribution.
- [x] Initialize `CHANGELOG.md` following "Keep a Changelog" and SemVer specifications.
- [x] Initialize `README.md` with project overview, tech stack, architecture breakdown, and CLI build instructions.

---

## 3. Solution & Infrastructure Configuration
- [x] Create `Directory.Build.props` at solution root configuring:
  - `<Nullable>enable</Nullable>`
  - `<ImplicitUsings>enable</ImplicitUsings>`
  - `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
  - `<LangVersion>latest</LangVersion>`
- [x] Verify project dependency tree across `OmniForge.sln`:
  - `OmniForge.Core` (Net 9 domain contracts & models)
  - `OmniForge.Services` (Net 9 / Windows App SDK service implementations)
  - `OmniForge.App` (WinUI 3 host, shell, views, and viewmodels)

---

## 4. Core Domain Contracts (`OmniForge.Core`)
- [x] Implement `Contracts/IToolModule.cs` plugin interface:
  - `Id`, `DisplayName`, `Description`, `IconGlyph`, `ViewType`, `ViewModelType`, `Priority`, `IsEnabled`.
- [x] Implement `Contracts/INavigationService.cs` shell navigation abstraction:
  - `NavigateTo(string)`, `NavigateTo(Type)`, `GoBack()`, `CanGoBack`, `CurrentPageType`, `Navigated` event.
- [x] Implement `Common/Result.cs` functional result wrapper (`Result`, `Result<T>`, `Error`).
- [x] Implement `Contracts/ILocalSettingsService.cs` configuration contract.

---

## 5. Service Implementations (`OmniForge.Services`)
- [x] Implement `Navigation/NavigationService.cs`:
  - Concrete `INavigationService` decoupling `Microsoft.UI.Xaml.Controls.Frame` from ViewModels.
- [x] Implement `Settings/LocalSettingsService.cs`:
  - Thread-safe JSON persistence in `%LocalAppData%\OmniForge\settings.json`.
- [x] Provide `ServiceCollectionExtensions.cs`:
  - Clean DI registration for core infrastructure services.

---

## 6. Presentation & Host (`OmniForge.App`)
- [x] Configure `App.xaml` & `App.xaml.cs`:
  - IoC container configuration with `Microsoft.Extensions.DependencyInjection`.
  - Service registration and module aggregation root.
- [x] Create `Extensions/ServiceCollectionExtensions.cs`:
  - Modular registration extensions for views, viewmodels, and navigation.
- [x] Implement `ViewModels/MainWindowViewModel.cs`:
  - Data binding for `NavigationView`, title bar state, back stack, and module activation.
- [x] Implement `Views/MainWindow.xaml` & `Views/MainWindow.xaml.cs`:
  - Windows 11 Fluent shell with `MicaAltBackdrop`, custom TitleBar, and `NavigationView`.
- [x] Verify complete build:
  - Zero warnings, zero errors with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

