---
name: security-and-input-hardening
description: Mandatory security coding workflow for OmniForge covering Path Traversal defense, ReDoS prevention, XML/JSON DoS limits, Symlink isolation, and safe secret storage.
---

# Security & Input Hardening Skill

Use this skill whenever writing code that accepts user paths, parses external files/payloads (JSON, XML, Regex, Archives), or stores configuration in **OmniForge**.

---

## 1. Directory Structure & Assets

- Audit Script: [audit_security_hardening.ps1](scripts/audit_security_hardening.ps1)
- Reference Implementation: [SecureInputHandlerExample.cs](examples/SecureInputHandlerExample.cs)
- Security Limits & Thresholds: [security-limits.json](resources/security-limits.json)

---

## 2. Path Traversal & File System Hardening (`SecurityGuard`)

1. **Always Canonicalize & Validate Paths via `SecurityGuard.ValidateSafePath`**:
   - Never pass raw user strings directly to `FileStream` or `File.Move`.
   - Always validate with `SecurityGuard.ValidateSafePath(userPath, allowedRootDirectory)` from `OmniForge.Core.Common`.
   - This automatically blocks null-byte injection (`\0`), Windows reserved device names (`CON`, `NUL`, `AUX`, `COM1`), and `..\` directory traversal outside the sandbox root.
2. **Skip Reparse Points & Symlinks in Recursive Scans**:
   - Always configure `EnumerationOptions` with `AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System` so malicious junction loops cannot trap OmniForge in infinite recursion or trick batch tools into modifying system directories.

---

## 3. Parser & Regex DoS Prevention (ReDoS, JSON/XML Bombs)

1. **Enforce Payload Size Limits First**:
   - Before parsing in-memory strings (JSON, XML, Base64, Diff), call `SecurityGuard.ValidatePayloadSize(input, maxBytes: 10 * 1024 * 1024)` to prevent Large Object Heap (LOH) exhaustion and `OutOfMemoryException`.
2. **Regex Timeout is Mandatory**:
   - Every runtime `new Regex(...)` with user-supplied patterns **must** specify a strict timeout (`TimeSpan.FromMilliseconds(250)`) and catch `RegexMatchTimeoutException`.
   - For static compile-time patterns, use `[GeneratedRegex("...", RegexOptions.NonBacktracking)]`.
3. **Safe XML & JSON Reader Limits**:
   - When using `XmlReader` or `XDocument`, always set `DtdProcessing = DtdProcessing.Prohibit` and `XmlResolver = null` to prevent XXE (XML External Entity) and Billion Laughs attacks.
   - When using `JsonDocument.Parse`, keep `MaxDepth = 64` (default) and never disable depth limits.

---

## 4. Sensitive Data & Logging Hygiene

1. **Never Log Raw User Secrets or JWT Signatures**:
   - In `FileLoggerProvider` or `ILogger<T>` calls, log metadata (file count, byte size, error code) rather than full file contents or raw bearer tokens.
2. **Never Store Secrets in Plaintext `settings.json`**:
   - `LocalSettingsService` (`%LocalAppData%\OmniForge\settings.json`) is strictly for non-sensitive UI preferences. If a future module requires API keys or tokens, encrypt them using Windows `PasswordVault` (`Windows.Security.Credentials`) or `ProtectedData` (DPAPI).

See [SecureInputHandlerExample.cs](examples/SecureInputHandlerExample.cs) for complete implementation patterns.
