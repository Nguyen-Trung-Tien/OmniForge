# OmniForge Clean Code & C# Engineering Standards

This document establishes the code quality, language feature usage, and architectural design standards for all C# code across the **OmniForge Utility Suite**.

---

## 1. SOLID Principles in Practice

### 1.1 Single Responsibility Principle (SRP)
- Each class must have a single reason to change.
- Separate business algorithms (Services), state representation & UI coordination (ViewModels), presentation layouts (Views/XAML), and contract specifications (Contracts).
- ViewModels must not perform direct disk I/O, database access, or network calls; they delegate to domain services via dependency injection.

### 1.2 Open/Closed Principle (OCP)
- OmniForge features must be extensible without modifying core shell logic.
- New utility tools are added by implementing `IToolModule` and registering their services in DI, without altering `MainWindow` or `MainWindowViewModel`.

### 1.3 Liskov Substitution Principle (LSP)
- Derived classes and module implementations must fulfill the complete behavior expected by their interface abstractions (`IToolModule`, `INavigationService`, `IFileSystemService`).
- Never throw `NotImplementedException` in production implementations of registered contracts.

### 1.4 Interface Segregation Principle (ISP)
- Interfaces must be small, focused, and cohesive.
- Split large interfaces into granular contracts (e.g., `IReadOnlyFileSystemService`, `IBatchFileProcessor`, `ILocalSettingsService`).
- Clients should not be forced to depend on methods they do not consume.

### 1.5 Dependency Inversion Principle (DIP)
- High-level modules must not depend on low-level modules; both must depend on abstractions defined in `OmniForge.Core.Contracts`.
- Concrete implementations are wired exclusively in the Composition Root (`App.xaml.cs` via `Microsoft.Extensions.DependencyInjection`).

---

## 2. Nullable Reference Types & Safety

OmniForge enforces `<Nullable>enable</Nullable>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

1. **No Null Suppressions Without Verification**:
   - Avoid using the null-forgiving operator (`!`) unless guaranteed by preceding validation or framework initialization mechanisms (e.g., source generator generated bindings).
2. **Explicit Nullability Declarations**:
   - Always declare parameters, return types, and properties with explicit nullability (`string?`, `T?`, `object?`).
   - Use `ArgumentNullException.ThrowIfNull(param)` or `ArgumentException.ThrowIfNullOrWhiteSpace(param)` at the entry point of public methods and constructors.
3. **Null-Coalescing and Null-Conditional Operators**:
   - Prefer `??`, `??=`, and `?.` over verbose `if (x != null)` chains.

---

## 3. Safe Exception Handling & Functional Result Pattern

1. **Operational Failures vs. System Defects**:
   - Expected failures (e.g., file not found, permission denied, invalid user input, parse failure) must return a functional `Result` or `Result<T>`.
   - Never throw exceptions for normal control flow.
2. **Safe Exception Wrapping**:
   - When calling external or Win32 APIs that throw exceptions, catch specific exception types and convert them into domain results:
     ```csharp
     public async Task<Result<string>> ReadFileContentAsync(string path, CancellationToken ct)
     {
         try
         {
             var content = await File.ReadAllTextAsync(path, ct);
             return Result<string>.Success(content);
         }
         catch (FileNotFoundException ex)
         {
             return Result<string>.Failure(new Error("File.NotFound", ex.Message));
         }
         catch (UnauthorizedAccessException ex)
         {
             return Result<string>.Failure(new Error("File.AccessDenied", ex.Message));
         }
         catch (Exception ex)
         {
             return Result<string>.Failure(new Error("File.UnexpectedError", ex.Message));
         }
     }
     ```
3. **No Empty Catch Blocks**:
   - Catching `Exception` and swallowing it without logging or diagnostic reporting is strictly forbidden.

---

## 4. Naming Conventions & Code Style

| Element | Convention | Example |
| :--- | :--- | :--- |
| Classes, Records, Structs | PascalCase | `ToolRegistryService`, `FileEntry` |
| Interfaces | PascalCase with `I` prefix | `IToolModule`, `INavigationService` |
| Methods | PascalCase, async ends in `Async` | `ProcessBatchAsync`, `RefreshItems` |
| Properties | PascalCase | `DisplayName`, `CanGoBack` |
| Private backing fields | `_camelCase` | `_navigationService`, `_isRunning` |
| Method parameters | camelCase | `cancellationToken`, `targetPath` |
| Local variables | camelCase | `fileStream`, `result` |
| Constants | PascalCase | `DefaultBufferSize`, `SettingsFileName` |

---

## 5. Modern C# 12 / 13 Language Features

OmniForge utilizes modern C# compiler capabilities (`<LangVersion>latest</LangVersion>`):
- **File-scoped namespaces**: All files must use `namespace OmniForge.Services;` without enclosing braces.
- **Primary Constructors**: Use primary constructors for dependency injection on classes and records where concise:
  ```csharp
  public sealed class LocalSettingsService(IFileSystemService fileSystem) : ILocalSettingsService
  {
      // ...
  }
  ```
- **Collection Expressions**: Use `[]` syntax for empty or inline collections:
  ```csharp
  public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = [];
  Span<byte> buffer = [0, 1, 2, 3];
  ```
- **Pattern Matching**: Prefer expressive pattern matching (`is not null`, `switch` expressions, tuple patterns) over deep nested `if-else` blocks.

