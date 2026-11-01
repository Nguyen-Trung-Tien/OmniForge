# OmniForge Agent Governance & System Architecture Protocol

This protocol defines the non-negotiable architectural laws, coding standards, and operational guidelines for all AI agents, engineers, and contributors working on the **OmniForge Utility Suite** codebase (`OmniForge.sln`).

---

## 1. Architectural Boundaries (Modular Clean Architecture)

OmniForge is structured into strictly decoupled layers with unidirectional dependency flow:

```
[ OmniForge.App ] (WinUI 3 Shell & Views, DI Host, Composition Root)
       │
       ▼
[ OmniForge.Services ] (Implementations: Navigation, Settings, File I/O, Tool Registry)
       │
       ▼
[ OmniForge.Core ] (Contracts: IToolModule, INavigationService, Result<T>, Models)
```

### 1.1 Dependency Hierarchy Rules
1. **OmniForge.Core** is completely agnostic of UI frameworks, Windows App SDK, and concrete service implementations. It defines domain abstractions, plugin contracts (`IToolModule`), functional results (`Result<T>`), and common models.
2. **OmniForge.Services** references `OmniForge.Core` and provides concrete cross-cutting implementations (settings persistence, navigation decoupling, registry services, file system utilities). It must never reference `OmniForge.App`.
3. **OmniForge.App** is the presentation layer and composition root. It references `OmniForge.Core` and `OmniForge.Services`. It hosts `App.xaml`, `MainWindow`, view models, and modular tool pages.
4. **Circular Dependencies Are Prohibited**: Under no circumstance should a lower layer reference an upper layer.
5. **Plugin-First Isolation**: Every utility tool inside OmniForge must implement `IToolModule`. Modules must be independently discoverable, instantiable, and capable of failing without compromising the shell or adjacent modules.

---

## 2. Presentation & MVVM Discipline: Zero Code-Behind Logic

1. **Pure Code-Behind**:
   - Code-behind files (`*.xaml.cs`) must contain **zero** business logic, validation rules, data transformation, or navigation redirection.
   - Permitted code in `*.xaml.cs` is strictly limited to:
     - Component initialization (`InitializeComponent()`).
     - Setting up the view model via DI: `public MyViewModel ViewModel { get; }`.
     - Window chrome and title bar interoperability (`ExtendsContentIntoTitleBar`, `AppWindow.TitleBar`).
2. **Compile-Time Data Binding**:
   - Always use `{x:Bind}` with explicit `Mode` (`OneWay` for read-only observables, `TwoWay` for interactive controls like `TextBox.Text`).
3. **Source Generators & Base Class Discipline**:
   - All ViewModels must derive from `ToolViewModelBase` (for utility modules) or `ObservableObject` (for shell items) and be declared `public partial class`.
   - Never write boilerplate `INotifyPropertyChanged` implementations, manual `IsBusy`/`ErrorMessage` properties, or raw `ICommand` implementations. Use `[ObservableProperty]`, `[RelayCommand]`, and `ToolViewModelBase.RunSafeAsync(...)`.
   - New `IToolModule` plugins are automatically discovered and registered by `ModuleAutoDiscoveryExtensions.AddDiscoveredToolModules`—do not hardcode module registrations in `ServiceCollectionExtensions.cs`.

---

## 3. Asynchronous & Non-Blocking File I/O Protocol

1. **No Synchronous Disk Access**:
   - Never call `File.ReadAllText`, `File.WriteAllText`, `Directory.GetFiles`, or synchronous stream methods on or near the UI thread.
   - Always use asynchronous equivalents: `File.ReadAllTextAsync`, `File.OpenRead`, or the injected `IFileSystemService`.
2. **Mandatory CancellationToken Propagation**:
   - Every asynchronous method must accept a `CancellationToken cancellationToken = default` parameter and propagate it down to the lowest I/O or Task API call.
3. **Streaming & Memory Efficiency**:
   - For batch or large file processing, avoid buffering entire datasets in memory.
   - Use `IAsyncEnumerable<T>`, chunked streams, and buffer pools (`ArrayPool<byte>.Shared`).
4. **Worker Thread Offloading**:
   - CPU-intensive tasks (hashing, checksums, image processing, parsing) must be scheduled onto the thread pool via `Task.Run` or dedicated background workers, keeping the UI thread responsive at 60+ FPS.

---

## 4. Error Isolation & Fault Tolerance

1. **Functional Result Pattern**:
   - Core and Service layer methods must return `Result` or `Result<T>` rather than throwing avoidable exceptions for expected operational failures (e.g., file not found, invalid formatting, access denied).
   - Reserve exceptions strictly for exceptional programming errors or unrecoverable system faults.
2. **Safe Exception Wrapping**:
   - Catch specific file system exceptions (`UnauthorizedAccessException`, `PathTooLongException`, `IOException`) and map them into structured `Result.Failure(Error)`.
3. **UI Crash Protection**:
   - In ViewModel commands, wrap external service calls in appropriate try/catch blocks that update observable error states (e.g., `ErrorMessage`, `HasError`) without crashing the WinUI dispatcher.
4. **Module Fault Isolation**:
   - An unhandled failure in an individual tool module must not crash `MainWindow`. The host shell must capture module activation exceptions, display a localized infobar/error badge, and allow the user to continue using other utilities.

---

## 5. Changelog & Version Governance

1. **Changelog Protocol**:
   - Maintain `CHANGELOG.md` at the project root strictly adhering to the [Keep a Changelog](https://keepachangelog.com/) format.
   - Supported section categories:
     - `Added` for new features or modules.
     - `Changed` for changes in existing functionality.
     - `Deprecated` for soon-to-be removed features.
     - `Removed` for now removed features.
     - `Fixed` for any bug fixes.
     - `Security` in case of vulnerabilities.
2. **Semantic Versioning (SemVer)**:
   - OmniForge follows `MAJOR.MINOR.PATCH` versioning (`v0.1.0-alpha` for Phase 1).
   - Every pull request, subagent task, or architectural feature addition must include a corresponding update to `CHANGELOG.md` under `[Unreleased]` or the targeted milestone version.

