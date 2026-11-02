using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace OmniForge.Core.Common;

/// <summary>
/// Centralized security and input validation guardrails for paths, payloads, and untrusted user input.
/// Prevents Path Traversal, Reserved Device Name attacks, XML Entity Expansion (XXE/Billion Laughs), and Memory Exhaustion (DoS).
/// </summary>
public static class SecurityGuard
{
    public const int DefaultMaxPayloadBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// Canonicalizes a file/directory path and verifies it does not contain null bytes, reserved Windows device names,
    /// or escape an optional <paramref name="allowedRootDirectory"/> (Path Traversal protection).
    /// </summary>
    public static Result<string> ValidateSafePath(string? inputPath, string? allowedRootDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            return Result.Failure<string>("Security.EmptyPath", "Path cannot be empty or whitespace.");
        }

        if (inputPath.Contains('\0'))
        {
            return Result.Failure<string>("Security.NullByte", "Path contains illegal null-byte characters.");
        }

        try
        {
            var fullPath = Path.GetFullPath(inputPath);
            var root = Path.GetPathRoot(fullPath) ?? string.Empty;
            var relativePortion = fullPath[root.Length..];
            var segments = relativePortion.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                var stem = Path.GetFileNameWithoutExtension(segment.TrimEnd('.', ' '));
                if (!string.IsNullOrEmpty(stem) && ReservedDeviceNames.Contains(stem))
                {
                    return Result.Failure<string>("Security.ReservedDeviceName", $"'{stem}' is a reserved Windows device name.");
                }
            }

            if (!string.IsNullOrWhiteSpace(allowedRootDirectory))
            {
                var fullRoot = Path.GetFullPath(allowedRootDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                var normalizedCandidate = fullPath.EndsWith(Path.DirectorySeparatorChar)
                    ? fullPath
                    : fullPath + Path.DirectorySeparatorChar;

                if (!normalizedCandidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Failure<string>(
                        "Security.PathTraversal",
                        $"Access denied: Path '{fullPath}' escapes the allowed root directory '{allowedRootDirectory}'.");
                }
            }

            return Result.Success(fullPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Result.Failure<string>("Security.MalformedPath", ex.Message);
        }
    }

    /// <summary>
    /// Validates that an in-memory text payload does not exceed <paramref name="maxBytes"/> to prevent OutOfMemoryException / DoS.
    /// </summary>
    public static Result ValidatePayloadSize(string? payload, int maxBytes = DefaultMaxPayloadBytes)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return Result.Failure("Security.EmptyPayload", "Input payload cannot be empty.");
        }

        int byteCount = Encoding.UTF8.GetByteCount(payload);
        if (byteCount > maxBytes)
        {
            return Result.Failure(
                "Security.PayloadTooLarge",
                $"Payload size ({ByteSizeFormatter.FormatBytes(byteCount)}) exceeds maximum safe limit ({ByteSizeFormatter.FormatBytes(maxBytes)}).");
        }

        return Result.Success();
    }

    /// <summary>
    /// Creates a hardened <see cref="XmlReaderSettings"/> instance with DTD processing prohibited and external resolvers disabled.
    /// </summary>
    public static XmlReaderSettings CreateSafeXmlReaderSettings(bool isAsync = false, long maxCharactersInDocument = DefaultMaxPayloadBytes) => new()
    {
        Async = isAsync,
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersFromEntities = 1024,
        MaxCharactersInDocument = maxCharactersInDocument
    };

    /// <summary>
    /// Loads an <see cref="XDocument"/> asynchronously from a stream using hardened XXE/DoS-safe <see cref="XmlReaderSettings"/>.
    /// </summary>
    public static async Task<XDocument> LoadSafeXmlAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = XmlReader.Create(stream, CreateSafeXmlReaderSettings(isAsync: true));
        return await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
    }
}

