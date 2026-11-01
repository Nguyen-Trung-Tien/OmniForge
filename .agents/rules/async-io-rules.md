# OmniForge Asynchronous & Non-Blocking File I/O Rules

This document specifies mandatory protocols for all disk, stream, and file system operations within the **OmniForge Utility Suite**.

---

## 1. Zero Blocking on the UI Thread

### 1.1 Forbidden Synchronous APIs
The following APIs must **never** be called directly from ViewModels, code-behind, or UI event handlers:
- `File.ReadAllText`, `File.ReadAllLines`, `File.ReadAllBytes`
- `File.WriteAllText`, `File.WriteAllBytes`, `File.Copy`, `File.Move`, `File.Delete`
- `Directory.GetFiles`, `Directory.GetDirectories`, `Directory.EnumerateFiles`
- `Stream.Read`, `Stream.Write`, `Stream.Flush`
- Synchronous blocking waits: `.Wait()`, `.Result`, `.GetAwaiter().GetResult()`
- `Thread.Sleep`

### 1.2 Mandatory Asynchronous Replacements
Always use asynchronous counterparts with `ConfigureAwait(false)` in service layers:
```csharp
await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
var text = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
```

---

## 2. Cancellation Token Discipline

1. **Mandatory Parameter**:
   - Every method performing I/O, hashing, compression, or long-running computation must accept a `CancellationToken cancellationToken = default`.
2. **Cooperative Cancellation**:
   - Long loops must periodically evaluate `cancellationToken.ThrowIfCancellationRequested()` or check `cancellationToken.IsCancellationRequested`.
3. **Passing Tokens to Low-Level Calls**:
   - Always propagate the received token to underlying .NET APIs (`stream.ReadAsync(buffer, token)`, `Task.Delay(100, token)`).
4. **Clean Disposal on Cancellation**:
   - Utilize `await using` or standard `using` blocks to guarantee file handles, temporary files, and unmanaged resources are closed immediately upon cancellation.

---

## 3. Memory Streaming & Chunked I/O with `IAsyncEnumerable`

1. **Never Buffer Massive Datasets in RAM**:
   - When processing directories with thousands of files or multi-gigabyte log/data files, do not collect them into a giant `List<T>` before processing.
2. **Streaming via `IAsyncEnumerable<T>`**:
   - Expose file streaming and directory walks as asynchronous streams:
     ```csharp
     public async IAsyncEnumerable<FileEntry> EnumerateFilesAsync(
         string directoryPath, 
         string searchPattern, 
         [EnumeratorCancellation] CancellationToken cancellationToken = default)
     {
         var enumerationOptions = new EnumerationOptions
         {
             IgnoreInaccessible = true,
             RecurseSubdirectories = true,
             AttributesToSkip = FileAttributes.ReparsePoint
         };

         var enumerable = Directory.EnumerateFiles(directoryPath, searchPattern, enumerationOptions);

         foreach (var file in enumerable)
         {
             cancellationToken.ThrowIfCancellationRequested();
             yield return await CreateFileEntryAsync(file, cancellationToken).ConfigureAwait(false);
         }
     }
     ```
3. **Memory and Buffer Pooling**:
   - When reading chunks of raw binary streams, utilize `ArrayPool<byte>.Shared` or `Memory<byte>` slices to minimize garbage collection pauses.

---

## 4. Robust File System Exception Handling

File systems are inherently non-deterministic due to concurrent OS processes, permissions, antivirus locks, and network shares.

### 4.1 Exception Mapping Matrix
| System Exception | Root Cause | Handling Strategy |
| :--- | :--- | :--- |
| `UnauthorizedAccessException` | Access denied, system/read-only file, ACL restriction | Report error code `IO.AccessDenied`. Do not crash; skip or prompt for elevation if appropriate. |
| `PathTooLongException` | Windows MAX_PATH (260 char) limit exceeded | Enable long path support (`\\?\` prefix or manifest `longPathAware`). Fall back gracefully with `IO.PathTooLong`. |
| `DirectoryNotFoundException` / `FileNotFoundException` | File deleted or moved during processing | Return `Result.Failure` with error code `IO.NotFound`. |
| `IOException` (HResult: 0x80070020 - Sharing Violation) | File locked by another process (e.g. antivirus, editor) | Retry with exponential backoff (up to 3 attempts with 50-200ms jitter) before failing. |
| `OperationCanceledException` / `TaskCanceledException` | User cancelled operation | Propagate or return `Result.Cancelled()`. Reset progress indicators. |

### 4.2 Long Path Support Protocol
- The application manifest (`app.manifest`) includes `<longPathAware>true</longPathAware>`.
- In raw path utilities, ensure Windows extended path prefixes are recognized:
  ```csharp
  public static string NormalizeExtendedPath(string path)
  {
      if (path.Length >= 260 && !path.StartsWith(@"\\?\"))
      {
          return @"\\?\" + Path.GetFullPath(path);
      }
      return path;
  }
  ```

