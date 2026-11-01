# OmniForge WinUI 3 & MVVM Engineering Standards

This document establishes non-negotiable architectural patterns, coding conventions, and engineering constraints for the **OmniForge Utility Suite** WinUI 3 presentation layer.

---

## 1. MVVM Architecture & CommunityToolkit.Mvvm

### 1.1 Source Generators Mandatory
- **Properties**: Always use `[ObservableProperty]` on private backing fields or partial properties within partial ViewModel classes.
  ```csharp
  [ObservableProperty]
  private string _statusMessage = string.Empty;

  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(ProcessBatchCommand))]
  private bool _hasSelectedFiles;
  ```
- **Commands**: Always annotate methods with `[RelayCommand]`.
  - For asynchronous operations, annotate methods returning `Task` (e.g., `[RelayCommand] private async Task LoadDataAsync(CancellationToken token)`). This automatically generates an `IAsyncRelayCommand` with `IsRunning`, `ExecutionTask`, and concurrent execution protection.
  - When execution depends on state, use `[RelayCommand(CanExecute = nameof(CanExecuteAction))]`.
- **Inheritance**:
  - All ViewModels must inherit from `ObservableObject` (or `ObservableRecipient` / `ObservableValidator` when messaging or data annotations validation is needed).
  - All ViewModels must be declared as `public partial class`.

### 1.2 Zero Logic in Code-Behind
- **Code-Behind (`.xaml.cs`) Restriction**:
  - Code-behind files must contain **zero** business, validation, data transformation, or navigation logic.
  - Allowed contents in `.xaml.cs`:
    1. Standard constructor calling `InitializeComponent()`.
    2. Injected ViewModel property binding: `public MyViewModel ViewModel { get; }`.
    3. Window chrome setup (e.g., `ExtendsContentIntoTitleBar = true`, `SetTitleBar(AppTitleBar)`).
- **Event Handling**:
  - Do not hook `Click`, `Tapped`, or `SelectionChanged` to code-behind handler methods for business logic.
  - Bind UI events directly to commands via `Command="{x:Bind ViewModel.MyCommand}"` or use XAML event-to-command behaviors.
  - Bind properties directly to ViewModel properties using compile-time `{x:Bind}` with explicit `Mode` (`OneWay`, `TwoWay`).

---

## 2. Thread Dispatching & UI Thread Synchronization

### 2.1 DispatcherQueue Usage
- WinUI 3 controls and observable collections bound to the visual tree can only be modified from the UI thread.
- When background worker threads, file I/O callbacks, or background timers produce updates, marshal them to the UI thread using the window/page `DispatcherQueue`:
  ```csharp
  dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
  {
      Items.Add(newItem);
      ProgressValue = progress;
  });
  ```
- Do not block the dispatcher thread with `.GetAwaiter().GetResult()` or heavy synchronous operations.
- Inject a dispatcher wrapper or pass `DispatcherQueue` explicitly to ViewModels when background thread marshalling is required.

---

## 3. Windows 11 Fluent Design & Visual Styling

### 3.1 Material & Backdrop
- **Mica Backdrop**: Use `MicaBackdrop` (`Kind="BaseAlt"` or `Kind="Base"`) as the system backdrop on `MainWindow` to achieve native Windows 11 visual depth and acrylic-like translucency.
- Ensure the backdrop fallback color adapts gracefully when the window loses focus or when running on high-contrast configurations.

### 3.2 Theme Awareness (Light / Dark / High Contrast)
- Never hardcode color hex codes (e.g., `#FFFFFF` or `#000000`) for backgrounds, foregrounds, or borders.
- Always utilize WinUI Theme Resources:
  - `{ThemeResource ApplicationPageBackgroundThemeBrush}`
  - `{ThemeResource LayerFillColorDefaultBrush}`
  - `{ThemeResource TextFillColorPrimaryBrush}`
  - `{ThemeResource TextFillColorSecondaryBrush}`
  - `{ThemeResource AccentFillColorDefaultBrush}`
  - `{ThemeResource CardBackgroundFillColorDefaultBrush}`
- Test and support system theme changes (`ElementTheme.Light`, `ElementTheme.Dark`, `ElementTheme.Default`) in runtime without requiring application restart.

### 3.3 Typography & Iconography
- Use standard Windows 11 typography (`Segoe UI Variable Text`, `Segoe UI Variable Display`).
- Iconography must standardize on **Segoe Fluent Icons** glyphs (e.g., `\uE80F` for Home, `\uE8B7` for Tool, `\uE713` for Settings).
- Provide accessible descriptions (`AutomationProperties.Name` and `ToolTipService.ToolTip`) for all icon-only buttons and navigation entries.

---

## 4. Navigation & View Lifecycle

### 4.1 NavigationView Integration
- Use `NavigationView` in `PaneDisplayMode="Left"` with compact/expanded states.
- Bind `MenuItemsSource` to an observable collection of modules (`NavigationItemViewModel`).
- Handle page back-navigation with standard `CanGoBack` synchronization and back-stack history clearing when navigating to top-level roots.
- Decouple the UI `Frame` completely from ViewModels using `INavigationService`. ViewModels request navigation by string key, module ID, or View Type without referencing WinUI controls.
