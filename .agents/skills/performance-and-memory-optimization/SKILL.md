---
name: performance-and-memory-optimization
description: High-performance .NET 9 and WinUI 3 optimization workflow using BulkObservableCollection, ArrayPool<byte>, Span<T>, UI virtualization, and zero-allocation patterns.
---

# Performance & Memory Optimization Skill

Use this skill whenever implementing data-intensive ViewModels, file scanners, text processors, or UI lists in **OmniForge** to guarantee 60+ FPS responsiveness and minimal GC pause time.

---

## 1. Directory Structure & Assets

- Audit Script: [audit_memory_patterns.ps1](scripts/audit_memory_patterns.ps1)
- Reference Implementation: [HighPerformanceCollectionExample.cs](examples/HighPerformanceCollectionExample.cs)
- Performance Budgets: [performance-benchmarks.json](resources/performance-benchmarks.json)

---

## 2. UI Collection & Binding Performance (60 FPS Law)

1. **Never Call `ObservableCollection<T>.Add` in a Loop**:
   - Adding 100 items one-by-one triggers 100 separate WinUI layout passes and freezes the UI thread.
   - Always use `BulkObservableCollection<T>` (`OmniForge.Core.Common`) and call `ReplaceAll(batch)` or `AddRange(batch)` to fire a **single** `Reset` event.
   - Reference: see [HighPerformanceCollectionExample.cs](examples/HighPerformanceCollectionExample.cs).
2. **Compile-Time `{x:Bind}` Only**:
   - Never use reflection-based `{Binding}`. Always use `{x:Bind ..., Mode=OneWay}` (or `TwoWay` for user inputs) which generates static C# code at compile time with zero runtime reflection overhead.

---

## 3. Memory & Zero-Allocation Patterns (Avoid LOH > 85 KB)

1. **Pool Byte Buffers (`ArrayPool<byte>.Shared`)**:
   - Any buffer $\ge 4\text{ KB}$ used for file hashing, stream copying, or encoding must be rented via `ArrayPool<byte>.Shared.Rent(65536)` and returned in a `finally` block (`ArrayPool<byte>.Shared.Return(buffer)`).
   - Never allocate `new byte[1024 * 1024]` on the Large Object Heap (LOH).
2. **Use `Span<char>` and `string.Create` for Formatting**:
   - Use `ByteSizeFormatter.FormatBytes(bytes)` and `ByteSizeFormatter.FormatDuration(elapsed)` from `OmniForge.Core.Common` which leverage `string.Create(CultureInfo.InvariantCulture, ...)` without intermediate string concatenations.
3. **Always Append `.ConfigureAwait(false)` in `OmniForge.Services` & `OmniForge.Core`**:
   - Service and Core methods must never force continuations back onto the WinUI 3 UI thread. Reserve UI thread context strictly for ViewModel property updates.
