---
name: crypto-hash-engine
description: High-performance, zero-allocation asynchronous file and stream hashing patterns (SHA-256, SHA-512, MD5, IncrementalHash) using ArrayPool<byte> and Result<T>.
---

# Crypto & Checksum Hash Engine Skill

Use this skill when implementing file integrity validators, checksum comparators, duplicate file finders, or cryptographic digest utilities in **OmniForge**.

---

## 1. Directory Structure & Assets

- Benchmark Script: [benchmark_hash.ps1](scripts/benchmark_hash.ps1)
- Reference Implementation: [StreamHashEngine.cs](examples/StreamHashEngine.cs)
- Algorithm Specifications: [hash-algorithms.json](resources/hash-algorithms.json)

---

## 2. Zero-Allocation Streaming Hash Rules

1. **Never Call `File.ReadAllBytes` or `File.ReadAllBytesAsync`**: Hashing a 4 GB ISO file with `ReadAllBytesAsync` causes an immediate `OutOfMemoryException` or massive LOH GC pause.
2. **Use `IncrementalHash` + `ArrayPool<byte>.Shared`**: Rent a 64 KB buffer from `ArrayPool<byte>.Shared`, stream the file with `FileOptions.Asynchronous | FileOptions.SequentialScan`, and feed slices into `IncrementalHash.AppendData(ReadOnlySpan<byte>)`.
3. **Hex Formatting on .NET 9**: Always use `Convert.ToHexStringLower(hashBytes)` (or `Convert.ToHexString(hashBytes)`) instead of manual `StringBuilder` or `BitConverter.ToString().Replace("-", "")`.

---

## 3. Production Reference Pattern

The complete, zero-allocation streaming hash implementation is available in [StreamHashEngine.cs](examples/StreamHashEngine.cs).
