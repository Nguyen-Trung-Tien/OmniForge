using System.Text;
using OmniForge.Core.Common;

namespace OmniForge.Core.Modules.FolderCreator;

/// <summary>
/// High-performance parsing engine for batch folder structures from raw text.
/// Supports Flat List and Indented Tree Hierarchy modes, dynamic token interpolation,
/// and strict Windows path sanitization.
/// </summary>
public static class FolderParser
{
    private static readonly HashSet<char> IllegalChars =
    [
        '\\', '/', ':', '*', '?', '"', '<', '>', '|'
    ];

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// Parses raw multiline text into a tree of <see cref="FolderItemNode"/>s.
    /// </summary>
    public static Result<IReadOnlyList<FolderItemNode>> Parse(
        string? rawInput,
        FolderInputMode mode = FolderInputMode.TreeHierarchy,
        int startSequence = 1,
        DateTime? referenceTime = null)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return Result.Success<IReadOnlyList<FolderItemNode>>([]);
        }

        var lines = rawInput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return Result.Success<IReadOnlyList<FolderItemNode>>([]);
        }

        return mode switch
        {
            FolderInputMode.TreeHierarchy => ParseTreeHierarchy(lines, startSequence, referenceTime),
            FolderInputMode.Flat => ParseFlatList(lines, startSequence, referenceTime),
            _ => Result.Failure<IReadOnlyList<FolderItemNode>>("FolderParser.InvalidMode", $"Unsupported folder input mode '{mode}'.")
        };
    }

    /// <summary>
    /// Sanitizes an individual folder segment name by replacing illegal Windows characters
    /// (\ / : * ? " &lt; &gt; | and ASCII 0-31), stripping trailing dots/spaces, and escaping reserved device names.
    /// </summary>
    public static string SanitizeFolderName(string? rawName, char replacement = '_')
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Folder";
        }

        var sb = new StringBuilder(rawName.Length);
        foreach (char c in rawName)
        {
            if (c < 32 || IllegalChars.Contains(c) || Path.GetInvalidFileNameChars().Contains(c))
            {
                sb.Append(replacement);
            }
            else
            {
                sb.Append(c);
            }
        }

        string sanitized = sb.ToString().Trim(' ', '.');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "Folder";
        }

        if (ReservedDeviceNames.Contains(sanitized))
        {
            sanitized = $"_{sanitized.ToUpperInvariant()}";
        }

        return sanitized;
    }

    /// <summary>
    /// Generates a multiline series of folder names from a pattern (e.g. "Lesson_{seq:2}"), count, and start sequence.
    /// </summary>
    public static string GenerateSeriesText(string? pattern, int count, int startSequence = 1, bool appendToExisting = false, string? existingText = null)
    {
        string rawPattern = string.IsNullOrWhiteSpace(pattern) ? "Folder_{seq:2}" : pattern.Trim();
        if (!TokenEvaluator.HasSequenceToken(rawPattern))
        {
            rawPattern = $"{rawPattern}_{{seq:2}}";
        }

        int safeCount = Math.Clamp(count, 1, 500);
        var sb = new StringBuilder();

        if (appendToExisting && !string.IsNullOrWhiteSpace(existingText))
        {
            sb.AppendLine(existingText.TrimEnd('\r', '\n'));
        }

        for (int i = 0; i < safeCount; i++)
        {
            int seq = startSequence + i;
            string line = TokenEvaluator.Evaluate(rawPattern, seq);
            if (i < safeCount - 1)
            {
                sb.AppendLine(line);
            }
            else
            {
                sb.Append(line);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Adjusts the indentation of all non-empty lines by <paramref name="deltaSpaces"/> (positive to indent, negative to outdent).
    /// </summary>
    public static string AdjustIndentation(string? rawInput, int deltaSpaces)
    {
        if (string.IsNullOrWhiteSpace(rawInput) || deltaSpaces == 0)
        {
            return rawInput ?? string.Empty;
        }

        var lines = rawInput.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder(rawInput.Length + Math.Abs(deltaSpaces) * lines.Length);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                if (i < lines.Length - 1) sb.AppendLine();
                continue;
            }

            int currentSpaces = CalculateIndentColumn(line);
            int targetSpaces = Math.Max(0, currentSpaces + deltaSpaces);
            string trimmed = line.TrimStart(' ', '\t');
            string resultLine = new string(' ', targetSpaces) + trimmed;

            if (i < lines.Length - 1)
            {
                sb.AppendLine(resultLine);
            }
            else
            {
                sb.Append(resultLine);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Cleans, trims trailing whitespace, removes blank lines, and optionally sorts lines alphabetically.
    /// </summary>
    public static string NormalizeLines(string? rawInput, bool sortAlphabetically = false)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return string.Empty;
        }

        var lines = rawInput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.TrimEnd());

        if (sortAlphabetically)
        {
            lines = lines.OrderBy(l => l.TrimStart(), StringComparer.OrdinalIgnoreCase);
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Mode B: Parses indented hierarchy where 2 spaces, 4 spaces, or Tabs define parent-child relationships.
    /// </summary>
    private static Result<IReadOnlyList<FolderItemNode>> ParseTreeHierarchy(
        string[] lines,
        int startSequence,
        DateTime? referenceTime)
    {
        var rootNodes = new List<FolderItemNode>();
        var stack = new Stack<(int IndentColumn, FolderItemNode Node)>();
        int currentSequence = startSequence;

        for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
        {
            string rawLine = lines[lineIdx];
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            int indentColumn = CalculateIndentColumn(rawLine);
            string trimmedText = rawLine.Trim();

            // Interpolate dynamic tokens
            string evaluatedText = TokenEvaluator.Evaluate(trimmedText, currentSequence, referenceTime);
            if (TokenEvaluator.HasSequenceToken(trimmedText))
            {
                currentSequence++;
            }

            // Sanitize segment name
            string sanitizedName = SanitizeFolderName(evaluatedText);

            var node = new FolderItemNode
            {
                RawText = rawLine,
                EvaluatedName = sanitizedName,
                IndentLevel = indentColumn,
                LineNumber = lineIdx + 1,
                Status = FolderItemStatus.Pending
            };

            // Pop stack items that are deeper than or at the same indentation level
            while (stack.Count > 0 && stack.Peek().IndentColumn >= indentColumn)
            {
                stack.Pop();
            }

            if (stack.Count == 0)
            {
                rootNodes.Add(node);
            }
            else
            {
                var parent = stack.Peek().Node;
                parent.AddChild(node);
            }

            stack.Push((indentColumn, node));
        }

        return Result.Success<IReadOnlyList<FolderItemNode>>(rootNodes);
    }

    /// <summary>
    /// Mode A: Parses flat list where each line represents a folder name (or forward/back-slash subpath).
    /// </summary>
    private static Result<IReadOnlyList<FolderItemNode>> ParseFlatList(
        string[] lines,
        int startSequence,
        DateTime? referenceTime)
    {
        var rootNodes = new List<FolderItemNode>();
        int currentSequence = startSequence;

        for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
        {
            string rawLine = lines[lineIdx];
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            string trimmedText = rawLine.Trim();
            string evaluatedText = TokenEvaluator.Evaluate(trimmedText, currentSequence, referenceTime);
            if (TokenEvaluator.HasSequenceToken(trimmedText))
            {
                currentSequence++;
            }

            // Support subpaths separated by '/' or '\' in flat mode (e.g. "Assets/Icons/Small")
            var segments = evaluatedText.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            FolderItemNode? currentParent = null;

            for (int segIdx = 0; segIdx < segments.Length; segIdx++)
            {
                string sanitizedSeg = SanitizeFolderName(segments[segIdx]);

                if (segIdx == 0)
                {
                    // Check if root already has this top-level segment to group subfolders
                    var existingRoot = rootNodes.FirstOrDefault(r => string.Equals(r.EvaluatedName, sanitizedSeg, StringComparison.OrdinalIgnoreCase));
                    if (existingRoot is not null && segments.Length > 1)
                    {
                        currentParent = existingRoot;
                    }
                    else
                    {
                        var rootNode = new FolderItemNode
                        {
                            RawText = rawLine,
                            EvaluatedName = sanitizedSeg,
                            IndentLevel = 0,
                            LineNumber = lineIdx + 1,
                            Status = FolderItemStatus.Pending
                        };
                        rootNodes.Add(rootNode);
                        currentParent = rootNode;
                    }
                }
                else
                {
                    // Check if currentParent already has this child
                    var existingChild = currentParent!.Children.FirstOrDefault(c => string.Equals(c.EvaluatedName, sanitizedSeg, StringComparison.OrdinalIgnoreCase));
                    if (existingChild is not null && segIdx < segments.Length - 1)
                    {
                        currentParent = existingChild;
                    }
                    else
                    {
                        var childNode = new FolderItemNode
                        {
                            RawText = rawLine,
                            EvaluatedName = sanitizedSeg,
                            IndentLevel = segIdx * 2,
                            LineNumber = lineIdx + 1,
                            Status = FolderItemStatus.Pending
                        };
                        currentParent.AddChild(childNode);
                        currentParent = childNode;
                    }
                }
            }
        }

        return Result.Success<IReadOnlyList<FolderItemNode>>(rootNodes);
    }

    /// <summary>
    /// Calculates visual indentation column from leading whitespace, treating tabs as 4 columns.
    /// </summary>
    private static int CalculateIndentColumn(string line)
    {
        int column = 0;
        foreach (char c in line)
        {
            if (c == ' ')
            {
                column++;
            }
            else if (c == '\t')
            {
                // Tab stops every 4 columns
                column = (column / 4 + 1) * 4;
            }
            else
            {
                break;
            }
        }
        return column;
    }
}
