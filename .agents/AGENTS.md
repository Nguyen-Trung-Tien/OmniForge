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
3. **Changelog Curation & Noise Elimination**:
   - **Pruning Permitted & Required**: Agents are explicitly permitted and expected to prune, merge, and delete unnecessary, redundant, or noisy changelog entries (e.g., intermediate debugging steps, micro-commits, duplicate bullet points).
   - **Zero Duplicate Headers**: Each release version block (`[Unreleased]` or `[X.Y.Z]`) must contain at most one of each category header (`### Added`, `### Changed`, `### Deprecated`, `### Removed`, `### Fixed`, `### Security`).
   - **Signal Over Noise**: Maintain clean, contributor- and end-user-facing release notes. Consolidate fragmented entries across multiple subagent runs into cohesive, high-impact summaries adhering to the `changelog-curator` skill.

---

## 6. Release & Deployment Protocol (`release/` Directory Governance)

This protocol governs the staging, packaging, and distribution of production deliverables for the OmniForge Utility Suite.

### 6.1 Distribution Root (`release/`) Architecture
The top-level `release/` directory is the authoritative distribution repository for end-user packages, strictly segregated from intermediate build output (`bin/`, `obj/`, `artifacts/publish/`):

```
release/
├── installer/             # Production Windows Setup Executables (Inno Setup)
│   └── OmniForge-Setup-v{Version}.exe
├── portable/              # Self-contained portable ZIP distributions
│   └── OmniForge-v{Version}-{platform}.zip
└── checksums.txt          # Cryptographic SHA-256 integrity verification hashes
```

### 6.2 Packaging & Deployment Pipeline
1. **Packaging Automation**:
   - Primary CLI command: `.\forge package -Platform x64`
   - Standalone Batch packager: `.\package.bat`
   - Dedicated PowerShell script: `pwsh -File .agents/skills/msix-release-packager/scripts/publish_release.ps1`
2. **Release Artifact Integrity Law**:
   - Every release package must include compiled XAML binaries (`.xbf`) in their respective directories (`Controls`, `Modules`, `Styles`, `Views`) and the Package Resource Index (`resources.pri` & `OmniForge.App.pri`).
   - Binaries must be published with `<PublishTrimmed>False</PublishTrimmed>` and `<PublishReadyToRun>True</PublishReadyToRun>`.
   - Packaging must always be preceded by a successful `.\forge verify` run (100% pass on all `NetArchTest` and `xUnit` test suites with zero compiler warnings).
3. **Automated Deployment & Checksums**:
   - Successful packaging automatically mirrors output binaries into `release/installer/` and appends/updates the SHA-256 signature in `release/checksums.txt`.
   - The `release/` directory is ignored by Git (`.gitignore`) to preserve repository cleanliness while serving as the standardized deployment directory.

---

## 7. Agent Skill Creation & Extension Protocol

This protocol defines the non-negotiable standards for creating, structuring, and registering AI Agent Skills within OmniForge.

### 7.1 Mandatory Reading Before Creation
- **Mandatory Prerequisite**: Prior to authoring, scaffolding, or implementing any new agent skill, the agent **MUST** read the official Antigravity skill documentation:
  - Official Antigravity Documentation: [https://antigravity.google/docs/skills/](https://antigravity.google/docs/skills/)
  - Local Built-in Customization Reference: `~/.gemini/antigravity/builtin/skills/agy-customizations/docs/skills.md`

### 7.2 Standard Skill Directory Architecture
Every skill in OmniForge must adhere strictly to the modular 4-part directory structure:

```
my-skill/
├── SKILL.md       # Required: Main skill instructions and YAML frontmatter metadata
├── scripts/       # Optional: Executable automation scripts and helper tools
├── examples/      # Optional: Reference implementations and code samples
└── resources/     # Optional: Templates, schemas, or data files
```

> [!NOTE]
> Antigravity defaults to `.agents/skills`, but still maintains backward compatibility for `.agent/skills`.
> Within the OmniForge repository, all skills must be placed under `.agents/skills/`.

### 7.3 `SKILL.md` Specification
1. **YAML Frontmatter (Required)**:
   ```yaml
   ---
   name: <skill-name>
   description: <Clear, third-person description of WHAT the skill does and WHEN to activate it>
   ---
   ```
   - `name`: Lowercase, hyphenated (kebab-case).
   - `description`: Informative and concise, explaining exact trigger conditions so the agent knows when to activate it.
2. **Progressive Disclosure**:
   - Keep `SKILL.md` compact and focused on procedure and orchestration.
   - Offload large templates, code files, and JSON schemas into `resources/`, `examples/`, or `scripts/` to conserve LLM token budget.

### 7.4 Skill Registration & Documentation Law
Whenever a new skill is created:
1. **Update `AGENTS.md`**: Ensure any new architectural or operational guidelines introduced by the skill are reflected in this master protocol.
2. **Update Applicable Usage Locations**:
   - Register the skill in `.agents/skills/cli-task-runner/SKILL.md` and `forge.ps1` if it introduces CLI commands or tasks.
   - Record the operational pattern in `.agents/MEMORY.md`.
   - Update `CHANGELOG.md` under `[Unreleased] -> ### Added`.
   - Verify inventory via `.\forge doctor`.

### 7.5 Automatic Issue-to-Skill Distillation Law
- **Mandate**: Whenever a non-trivial runtime error, architecture race condition, platform quirk, or unique technical issue is diagnosed and resolved, the agent **MUST** package the solution into a dedicated skill via `issue-skill-distiller` (or `.\forge distill-issue <Name>`).
- This guarantees continuous, autonomous agent evolution: future agent runs can immediately identify similar issues, apply verified runbooks, and avoid repeating root-cause investigations.



