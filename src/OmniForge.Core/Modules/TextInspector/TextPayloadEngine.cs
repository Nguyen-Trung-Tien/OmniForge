using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using OmniForge.Core.Common;

namespace OmniForge.Core.Modules.TextInspector;

/// <summary>
/// High-performance engine for JSON/XML formatting/minification, Base64/URL encoding/decoding,
/// JWT payload inspection, and line diff computation.
/// </summary>
public static class TextPayloadEngine
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    public static async Task<Result<string>> FormatJsonAsync(string input, bool indented = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Failure<string>("Json.Empty", "JSON payload cannot be empty.");

        return await Task.Run(() =>
        {
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
            catch (Exception ex)
            {
                return Result.Failure<string>("Json.Error", ex.Message);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Result<string>> FormatXmlAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Failure<string>("Xml.Empty", "XML payload cannot be empty.");

        return await Task.Run(() =>
        {
            try
            {
                var doc = XDocument.Parse(input);
                return Result.Success(doc.ToString());
            }
            catch (Exception ex)
            {
                return Result.Failure<string>("Xml.SyntaxError", ex.Message);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public static Result<string> EncodeBase64(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return Result.Failure<string>("Base64.Empty", "Input text cannot be empty.");

        try
        {
            return Result.Success(Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText)));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>("Base64.Error", ex.Message);
        }
    }

    public static Result<string> DecodeBase64(string base64Text)
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
        catch (Exception ex)
        {
            return Result.Failure<string>("Base64.Error", ex.Message);
        }
    }

    public static Result<string> EncodeUrl(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return Result.Failure<string>("Url.Empty", "Input text cannot be empty.");

        try
        {
            return Result.Success(Uri.EscapeDataString(plainText));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>("Url.Error", ex.Message);
        }
    }

    public static Result<string> DecodeUrl(string urlEncodedText)
    {
        if (string.IsNullOrEmpty(urlEncodedText))
            return Result.Failure<string>("Url.Empty", "URL-encoded input cannot be empty.");

        try
        {
            return Result.Success(Uri.UnescapeDataString(urlEncodedText));
        }
        catch (Exception ex)
        {
            return Result.Failure<string>("Url.Error", ex.Message);
        }
    }

    public static Result<JwtInspectionResult> InspectJwt(string jwtToken)
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
            return Result.Failure<JwtInspectionResult>("Jwt.Empty", "JWT token cannot be empty.");

        var parts = jwtToken.Trim().Split('.');
        if (parts.Length < 2)
            return Result.Failure<JwtInspectionResult>("Jwt.InvalidFormat", "JWT must contain at least Header.Payload segments separated by dots.");

        try
        {
            string headerJson = DecodeBase64UrlSegment(parts[0]);
            using var headerDoc = JsonDocument.Parse(headerJson);
            string formattedHeader = JsonSerializer.Serialize(headerDoc.RootElement, IndentedJsonOptions);

            string payloadJson = DecodeBase64UrlSegment(parts[1]);
            using var payloadDoc = JsonDocument.Parse(payloadJson);
            string formattedPayload = JsonSerializer.Serialize(payloadDoc.RootElement, IndentedJsonOptions);

            string signature = parts.Length >= 3 ? parts[2] : "(no signature)";

            return Result.Success(new JwtInspectionResult(formattedHeader, formattedPayload, signature));
        }
        catch (Exception ex)
        {
            return Result.Failure<JwtInspectionResult>("Jwt.DecodeFailed", $"Failed to parse JWT: {ex.Message}");
        }
    }

    private static string DecodeBase64UrlSegment(string base64Url)
    {
        string padded = base64Url.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        byte[] bytes = Convert.FromBase64String(padded);
        return Encoding.UTF8.GetString(bytes);
    }

    public static IReadOnlyList<DiffLine> ComputeLineDiff(string leftText, string rightText)
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

