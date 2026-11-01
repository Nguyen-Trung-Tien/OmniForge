---
name: regex-bulk-renamer
description: Safe, ReDoS-protected Regex bulk file renaming, token interpolation ({index}, {date}, {parent}), dry-run preview collision detection, and transactional Undo Journal rollback for Phase 2.
---

# Regex Bulk File Renamer & Undo Journal Skill

Use this skill when implementing the **Batch File & Path Renamer** module (Phase 2 Roadmap). File renaming mutates user disk state and **must** enforce pre-flight collision checks, ReDoS regex timeouts, and a reversible Undo Journal.

---

## 1. Directory Structure & Assets

- ReDoS Test Script: [test_regex_redos.ps1](scripts/test_regex_redos.ps1)
- Reference Engine: [BulkRenameEngine.cs](examples/BulkRenameEngine.cs)
- Token Catalog: [rename-tokens.json](resources/rename-tokens.json)

---

## 2. Non-Negotiable Safety Laws for Bulk Renaming

1. **Dry-Run Preview First**: Never rename files on disk without first computing a deterministic `IReadOnlyList<RenamePreviewItem>` and checking for duplicate target paths or invalid Windows filename characters (`Path.GetInvalidFileNameChars()`).
2. **ReDoS Timeout Guard**: User-supplied regular expressions must always use `RegexOptions.None` (or `IgnoreCase`) with an explicit `matchTimeout: TimeSpan.FromMilliseconds(250)` to prevent catastrophic backtracking from freezing worker threads.
3. **Transactional Undo Journal**: Every executed batch rename must return an `UndoBatchRecord` (or persist to `%LocalAppData%\OmniForge\journals\`) so the user can revert the batch in reverse topological order with one click.

---

## 3. Production Reference Implementation

See [BulkRenameEngine.cs](examples/BulkRenameEngine.cs) for full collision detection, ReDoS timeout, and token replacement logic.
