---
name: text-payload-inspector
description: High-performance, zero-allocation JSON/XML validation, formatting, minification, Base64/URL/JWT decoding, and line-by-line text diffing patterns for Phase 2 Text & Payload Inspector.
---

# Text & Payload Inspector Skill

Use this skill when building or extending the **Text & Payload Inspector** module (Phase 2 Roadmap) or any structured text transformation utility in **OmniForge**.

---

## 1. Directory Structure & Assets

- Benchmark Script: [benchmark_payload_parsing.ps1](scripts/benchmark_payload_parsing.ps1)
- Reference Implementation: [TextPayloadService.cs](examples/TextPayloadService.cs)
- Sample Payloads: [payload-templates.json](resources/payload-templates.json)

---

## 2. Architectural Rules for Payload Processing

1. **Offload Parsing for Payloads > 16 KB**: Always wrap parsing, formatting, and diffing inside `Task.Run(..., cancellationToken)` so large JSON/XML documents never block the WinUI 3 UI thread.
2. **Streamed UTF-8 JSON Formatting**: Prefer `Utf8JsonWriter` + `JsonDocument` over reflection-heavy `JsonSerializer.Deserialize<object>()` for 3x faster formatting and minification with minimal GC pressure.
3. **Never Throw on Malformed User Input**: Catch `JsonException`, `XmlException`, and `FormatException` and map them to structured `Result.Failure<string>("Payload.Invalid", ex.Message)` with line and byte position details.

---

## 3. Production Reference Implementation

See [TextPayloadService.cs](examples/TextPayloadService.cs) for full JSON, XML, Base64, JWT, and Diff algorithms.
