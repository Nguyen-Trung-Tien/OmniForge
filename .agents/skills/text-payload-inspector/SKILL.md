---
name: text-payload-inspector
description: High-performance, zero-allocation JSON/XML validation, formatting, minification, Base64/URL/JWT decoding, and line-by-line text diffing patterns for Phase 2 Text & Payload Inspector.
---

# Text & Payload Inspector Skill

Use this skill when building or extending the **Text & Payload Inspector** module (Phase 2 Roadmap) or any structured text transformation utility in **OmniForge**.

---

## 1. Architectural Rules for Payload Processing

1. **Offload Parsing for Payloads > 16 KB**: Always wrap parsing, formatting, and diffing inside `Task.Run(..., cancellationToken)` so large JSON/XML documents never block the WinUI 3 UI thread.
2. **Streamed UTF-8 JSON Formatting**: Prefer `Utf8JsonWriter` + `JsonDocument` over reflection-heavy `JsonSerializer.Deserialize<object>()` for 3x faster formatting and minification with minimal GC pressure.
3. **Never Throw on Malformed User Input**: Catch `JsonException`, `XmlException`, and `FormatException` and map them to structured `Result.Failure<string>("Payload.Invalid", ex.Message)` with line and byte position details.

---

## 2. Production Reference Implementation (`ITextPayloadService`)

```csharp
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using OmniForge.Core.Common;

namespace OmniForge.App.Modules.TextInspector;

public sealed record DiffLine(int LineNumber, string Prefix, string Content);

public interface ITextPayloadService
{
    Task<Result<string>> FormatJsonAsync(string input, bool indented = true, CancellationToken ct = default);
    Task<Result<string>> FormatXmlAsync(string input, CancellationToken ct = default);
    Result<string> EncodeBase64(string plainText);
    Result<string> DecodeBase64(string base64Text);
    Result<string> InspectJwtPayload(string jwtToken);
    IReadOnlyList<DiffLine> ComputeLineDiff(string leftText, string rightText);
}

public sealed class TextPayloadService : ITextPayloadService
{
    public Task<Result<string>> FormatJsonAsync(string input, bool indented = true, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(input))
                return Result.Failure<string>("Json.Empty", "JSON payload cannot be empty.");

            try
            {
                using var doc = JsonDocument.Parse(input, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });

                var buffer = new ArrayBufferWriter<byte>();
                using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = indented }))
                {
                    doc.RootElement.WriteTo(writer);
                }

                return Result.Success(Encoding.UTF8.GetString(buffer.WrittenSpan));
            }
            catch (JsonException ex)
            {
                return Result.Failure<string>(
                    "Json.SyntaxError",
                    $"Line {(ex.LineNumber ?? 0) + 1}, Pos {ex.BytePositionInLine ?? 0}: {ex.Message}");
            }
        }, ct);

    public Task<Result<string>> FormatXmlAsync(string input, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(input))
                return Result.Failure<string>("Xml.Empty", "XML payload cannot be empty.");

            try
            {
                var doc = XDocument.Parse(input);
                return Result.Success(doc.ToString());
            }
            catch (Exception ex)
            {
                return Result.Failure<string>("Xml.SyntaxError", ex.Message);
            }
        }, ct);

    public Result<string> EncodeBase64(string plainText) =>
        string.IsNullOrEmpty(plainText)
            ? Result.Failure<string>("Base64.Empty", "Input text cannot be empty.")
            : Result.Success(Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText)));

    public Result<string> DecodeBase64(string base64Text)
    {
        if (string.IsNullOrWhiteSpace(base64Text))
            return Result.Failure<string>("Base64.Empty", "Base64 input cannot be empty.");

        try
        {
            var bytes = Convert.FromBase64String(base64Text.Trim());
            return Result.Success(Encoding.UTF8.GetString(bytes));
        }
        catch (FormatException ex)
        {
            return Result.Failure<string>("Base64.Invalid", ex.Message);
        }
    }

    public Result<string> InspectJwtPayload(string jwtToken)
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
            return Result.Failure<string>("Jwt.Empty", "JWT token cannot be empty.");

        var parts = jwtToken.Trim().Split('.');
        if (parts.Length < 2)
            return Result.Failure<string>("Jwt.InvalidFormat", "JWT must contain at least Header.Payload segments.");

        try
        {
            var padded = parts[1].Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            using var doc = JsonDocument.Parse(json);
            return Result.Success(JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>("Jwt.DecodeFailed", ex.Message);
        }
    }

    public IReadOnlyList<DiffLine> ComputeLineDiff(string leftText, string rightText)
    {
        var leftLines = (leftText ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        var rightLines = (rightText ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        var max = Math.Max(leftLines.Length, rightLines.Length);
        var output = new List<DiffLine>(max);

        for (int i = 0; i < max; i++)
        {
            var l = i < leftLines.Length ? leftLines[i] : null;
            var r = i < rightLines.Length ? rightLines[i] : null;

            if (l == r)
            {
                output.Add(new DiffLine(i + 1, " ", l ?? string.Empty));
            }
            else
            {
                if (l is not null) output.Add(new DiffLine(i + 1, "-", l));
                if (r is not null) output.Add(new DiffLine(i + 1, "+", r));
            }
        }

        return output;
    }
}
```

