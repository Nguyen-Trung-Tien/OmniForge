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
            byte[] bytes = Convert.FromBase64String(base64Text.Trim());
            return Result.Success(Encoding.UTF8.GetString(bytes));
        }
        catch (FormatException ex)
        {
            return Result.Failure<string>("Base64.InvalidFormat", ex.Message);
        }
    }

    public Result<string> InspectJwtPayload(string jwtToken)
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
            return Result.Failure<string>("Jwt.Empty", "JWT cannot be empty.");

        string[] parts = jwtToken.Trim().Split('.');
        if (parts.Length < 2)
            return Result.Failure<string>("Jwt.InvalidFormat", "JWT must contain at least Header and Payload parts separated by dots.");

        try
        {
            string payloadRaw = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payloadRaw.Length % 4)
            {
                case 2: payloadRaw += "=="; break;
                case 3: payloadRaw += "="; break;
            }

            byte[] bytes = Convert.FromBase64String(payloadRaw);
            using var doc = JsonDocument.Parse(bytes);
            return Result.Success(JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>("Jwt.DecodeFailed", ex.Message);
        }
    }

    public IReadOnlyList<DiffLine> ComputeLineDiff(string leftText, string rightText)
    {
        string[] leftLines = leftText.Split(["\r\n", "\n"], StringSplitOptions.None);
        string[] rightLines = rightText.Split(["\r\n", "\n"], StringSplitOptions.None);

        var list = new List<DiffLine>();
        int max = Math.Max(leftLines.Length, rightLines.Length);

        for (int i = 0; i < max; i++)
        {
            string left = i < leftLines.Length ? leftLines[i] : string.Empty;
            string right = i < rightLines.Length ? rightLines[i] : string.Empty;

            if (string.Equals(left, right, StringComparison.Ordinal))
            {
                list.Add(new DiffLine(i + 1, " ", left));
            }
            else
            {
                if (i < leftLines.Length) list.Add(new DiffLine(i + 1, "-", left));
                if (i < rightLines.Length) list.Add(new DiffLine(i + 1, "+", right));
            }
        }

        return list;
    }
}

