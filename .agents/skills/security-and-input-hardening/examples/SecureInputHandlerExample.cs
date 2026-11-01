using System.Text.RegularExpressions;
using System.Xml;
using OmniForge.Core.Common;

namespace OmniForge.Services.Security;

public static class SecureInputHandlerExample
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    public static Result<string> ReadUserFileSafely(string userSuppliedPath, string sandboxDirectory)
    {
        // 1. Path traversal defense
        var safePathResult = SecurityGuard.ValidateSafePath(userSuppliedPath, sandboxDirectory);
        if (safePathResult.IsFailure)
        {
            return Result.Failure<string>(safePathResult.Error);
        }

        return Result.Success(safePathResult.Value);
    }

    public static Result<string> ValidateAndProcessPayload(string rawPayload, int maxSizeBytes = 10 * 1024 * 1024)
    {
        // 2. LOH / DoS payload size check
        var sizeResult = SecurityGuard.ValidatePayloadSize(rawPayload, maxSizeBytes);
        if (sizeResult.IsFailure)
        {
            return Result.Failure<string>(sizeResult.Error);
        }

        return Result.Success(rawPayload);
    }

    public static Result<bool> MatchPatternWithReDoSGuard(string input, string pattern)
    {
        // 3. Strict ReDoS timeout
        try
        {
            var regex = new Regex(pattern, RegexOptions.None, RegexTimeout);
            return Result.Success(regex.IsMatch(input));
        }
        catch (RegexMatchTimeoutException)
        {
            return Result.Failure<bool>("Security.RegexTimeout", "Regular expression evaluation exceeded 250ms limit.");
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<bool>("Security.InvalidPattern", ex.Message);
        }
    }

    public static XmlReaderSettings CreateHardenedXmlReaderSettings()
    {
        // 4. XXE & XML Entity Bomb defense
        return new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 1024
        };
    }
}

