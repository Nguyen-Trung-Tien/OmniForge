using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
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
    /// Mode B: Parses indented hierarchy where 2 spaces, 4 spaces, Tabs, ASCII tree symbols,
    /// or markdown bullets define parent-child relationships.
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

            var (indentColumn, cleanLine) = ExtractTreeLine(rawLine);
            if (string.IsNullOrWhiteSpace(cleanLine))
            {
                continue;
            }

            // Interpolate dynamic tokens
            string evaluatedText = TokenEvaluator.Evaluate(cleanLine, currentSequence, referenceTime);
            if (TokenEvaluator.HasSequenceToken(cleanLine))
            {
                currentSequence++;
            }

            // Split into subsegments if line contains forward or backward slashes (e.g. "src/components")
            var segments = evaluatedText.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            // Pop stack items that are deeper than or at the same indentation level
            while (stack.Count > 0 && stack.Peek().IndentColumn >= indentColumn)
            {
                stack.Pop();
            }

            FolderItemNode? currentParent = stack.Count > 0 ? stack.Peek().Node : null;

            for (int segIdx = 0; segIdx < segments.Length; segIdx++)
            {
                string sanitizedSeg = SanitizeFolderName(segments[segIdx]);
                int effectiveIndent = indentColumn + (segIdx * 2);

                var node = new FolderItemNode
                {
                    RawText = rawLine,
                    EvaluatedName = sanitizedSeg,
                    IndentLevel = effectiveIndent,
                    LineNumber = lineIdx + 1,
                    Status = FolderItemStatus.Pending
                };

                if (currentParent is null)
                {
                    rootNodes.Add(node);
                }
                else
                {
                    currentParent.AddChild(node);
                }

                currentParent = node;

                // Push to stack only the last segment of this line at its effective indent
                if (segIdx == segments.Length - 1)
                {
                    stack.Push((effectiveIndent, node));
                }
            }
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
    /// Parses plain text, CSV, or TSV lines into a clean list of folder strings.
    /// Supports comma-, semicolon-, tab- or newline-separated values with optional header filtering.
    /// </summary>
    public static Result<IReadOnlyList<string>> ParseLinesFromText(string? rawText, bool sanitize = true)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return Result.Failure<IReadOnlyList<string>>("FolderParser.EmptyText", "Text cannot be empty.");
        }

        var lines = rawText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
        {
            return Result.Failure<IReadOnlyList<string>>("FolderParser.NoValidLines", "No valid lines found to import.");
        }

        var folderList = new List<string>(lines.Length);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Check if line 0 is a table header row (e.g. "Folder Name, Path" or "Thư mục, Ghi chú")
            if (i == 0 && IsLikelyHeaderRow(line))
            {
                continue;
            }

            // Check for delimited lines: Tab, Semicolon, or Comma (when not containing slash paths)
            char? delimiter = line.Contains('\t') ? '\t' : (line.Contains(';') ? ';' : (line.Contains(',') && !line.Contains('/') && !line.Contains('\\') ? ',' : null));
            if (delimiter.HasValue)
            {
                var parts = line.Split(delimiter.Value, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length > 1)
                {
                    // Check if one of the parts is numeric (e.g. "1, Client_A" in tabular CSV)
                    bool hasNumericCol = parts.Any(p => int.TryParse(p, out _));
                    if (hasNumericCol)
                    {
                        // Tabular row: select the meaningful non-numeric folder column
                        string candidate = parts.FirstOrDefault(p => !int.TryParse(p, out _) && !string.IsNullOrWhiteSpace(p)) ?? parts[0];
                        string cleaned = sanitize ? SanitizePathPreservingSeparators(candidate) : candidate.Trim();
                        if (!string.IsNullOrWhiteSpace(cleaned) && !IsLikelyHeaderRow(cleaned))
                        {
                            folderList.Add(cleaned);
                        }
                        continue;
                    }

                    // Otherwise, all parts are distinct folder names in a delimited list (e.g. "Photos, Videos, Audio")
                    foreach (var part in parts)
                    {
                        string cleaned = sanitize ? SanitizeFolderName(part) : part.Trim();
                        if (!string.IsNullOrWhiteSpace(cleaned) && !IsLikelyHeaderRow(cleaned))
                        {
                            folderList.Add(cleaned);
                        }
                    }
                    continue;
                }
            }

            // Case C: Single line folder path (e.g. "Parent/Child" or "Folder 1")
            string finalEntry = sanitize ? SanitizePathPreservingSeparators(line) : line.Trim();
            if (!string.IsNullOrWhiteSpace(finalEntry))
            {
                folderList.Add(finalEntry);
            }
        }

        if (folderList.Count == 0)
        {
            return Result.Failure<IReadOnlyList<string>>("FolderParser.NoItems", "Could not parse any valid folder names from input.");
        }

        return Result.Success<IReadOnlyList<string>>(folderList);
    }

    /// <summary>
    /// Parses Excel (.xlsx) OpenXML files asynchronously using native BCL ZipArchive and XDocument.
    /// Zero third-party dependencies, high performance, trimming-safe.
    /// </summary>
    public static async Task<Result<IReadOnlyList<string>>> ParseFromExcelStreamAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            // 1. Load shared strings table if present
            var sharedStrings = new List<string>();
            var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
            if (sharedStringsEntry is not null)
            {
                if (sharedStringsEntry.Length > SecurityGuard.DefaultMaxPayloadBytes)
                {
                    return Result.Failure<IReadOnlyList<string>>("Security.PayloadTooLarge", "Excel shared strings table exceeds maximum safe size limit.");
                }

                await using var ssStream = sharedStringsEntry.Open();
                using var ssReader = System.Xml.XmlReader.Create(ssStream, new System.Xml.XmlReaderSettings
                {
                    Async = true,
                    DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersFromEntities = 1024,
                    MaxCharactersInDocument = SecurityGuard.DefaultMaxPayloadBytes
                });
                var ssDoc = await XDocument.LoadAsync(ssReader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
                XNamespace ssNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                foreach (var si in ssDoc.Descendants(ssNs + "si"))
                {
                    string val = string.Concat(si.Descendants(ssNs + "t").Select(t => t.Value));
                    sharedStrings.Add(val);
                }
            }

            // 2. Locate first worksheet
            var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                ?? archive.Entries.FirstOrDefault(e => e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

            if (sheetEntry is null)
            {
                return Result.Failure<IReadOnlyList<string>>("FolderParser.NoWorksheet", "No valid worksheet found in the Excel workbook.");
            }

            if (sheetEntry.Length > SecurityGuard.DefaultMaxPayloadBytes)
            {
                return Result.Failure<IReadOnlyList<string>>("Security.PayloadTooLarge", "Excel worksheet exceeds maximum safe size limit.");
            }

            await using var sheetStream = sheetEntry.Open();
            using var sheetReader = System.Xml.XmlReader.Create(sheetStream, new System.Xml.XmlReaderSettings
            {
                Async = true,
                DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = 1024,
                MaxCharactersInDocument = SecurityGuard.DefaultMaxPayloadBytes
            });
            var sheetDoc = await XDocument.LoadAsync(sheetReader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
            XNamespace sNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            var rows = sheetDoc.Descendants(sNs + "row").ToList();
            if (rows.Count == 0)
            {
                return Result.Failure<IReadOnlyList<string>>("FolderParser.EmptyWorksheet", "The selected worksheet contains no rows.");
            }

            var folderList = new List<string>(rows.Count);
            int targetColIdx = 0;
            bool isFirstRow = true;

            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cells = row.Elements(sNs + "c").ToList();
                if (cells.Count == 0) continue;

                // On first row, check if any column header matches "Folder", "Name", "Path", "Thư mục", etc.
                if (isFirstRow)
                {
                    isFirstRow = false;
                    bool headerFound = false;
                    foreach (var cell in cells)
                    {
                        string cellRef = cell.Attribute("r")?.Value ?? string.Empty;
                        int colIdx = GetColumnIndex(cellRef);
                        string cellVal = GetCellValue(cell, sharedStrings, sNs);

                        if (IsLikelyHeaderRow(cellVal))
                        {
                            targetColIdx = colIdx;
                            headerFound = true;
                            break;
                        }
                    }

                    if (headerFound)
                    {
                        // Skip the header row itself
                        continue;
                    }
                }

                // Extract cell in targetColIdx
                string? folderName = null;
                foreach (var cell in cells)
                {
                    string cellRef = cell.Attribute("r")?.Value ?? string.Empty;
                    int colIdx = GetColumnIndex(cellRef);
                    if (colIdx == targetColIdx)
                    {
                        folderName = GetCellValue(cell, sharedStrings, sNs);
                        break;
                    }
                }

                // Fallback to first non-empty cell if target column had no value
                if (string.IsNullOrWhiteSpace(folderName) && cells.Count > 0)
                {
                    folderName = GetCellValue(cells[0], sharedStrings, sNs);
                }

                if (!string.IsNullOrWhiteSpace(folderName))
                {
                    string sanitized = SanitizePathPreservingSeparators(folderName.Trim());
                    if (!string.IsNullOrWhiteSpace(sanitized) && !IsLikelyHeaderRow(sanitized))
                    {
                        folderList.Add(sanitized);
                    }
                }
            }

            if (folderList.Count == 0)
            {
                return Result.Failure<IReadOnlyList<string>>("FolderParser.NoValidRows", "No valid folder entries could be parsed from the Excel file.");
            }

            return Result.Success<IReadOnlyList<string>>(folderList);
        }
        catch (InvalidDataException)
        {
            return Result.Failure<IReadOnlyList<string>>("FolderParser.CorruptedExcel", "The file is not a valid Excel (.xlsx) file or is corrupted.");
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<string>>("FolderParser.ExcelReadError", $"Failed to read Excel workbook: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports a list of folders from an Excel (.xlsx), CSV, TSV, or TXT file asynchronously.
    /// </summary>
    public static async Task<Result<IReadOnlyList<string>>> ImportFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Result.Failure<IReadOnlyList<string>>("FolderParser.InvalidPath", "File path cannot be empty.");
        }

        var pathCheck = SecurityGuard.ValidateSafePath(filePath);
        if (pathCheck.IsFailure)
        {
            return Result.Failure<IReadOnlyList<string>>(pathCheck.Error);
        }

        string safePath = pathCheck.Value;
        if (!File.Exists(safePath))
        {
            return Result.Failure<IReadOnlyList<string>>("FolderParser.FileNotFound", $"File not found: {safePath}");
        }

        var fileInfo = new FileInfo(safePath);
        if (fileInfo.Length > SecurityGuard.DefaultMaxPayloadBytes)
        {
            return Result.Failure<IReadOnlyList<string>>(
                "Security.PayloadTooLarge",
                $"File size ({ByteSizeFormatter.FormatBytes(fileInfo.Length)}) exceeds maximum safe limit ({ByteSizeFormatter.FormatBytes(SecurityGuard.DefaultMaxPayloadBytes)}).");
        }

        string ext = Path.GetExtension(safePath).ToLowerInvariant();
        if (ext == ".xlsx")
        {
            await using var stream = new FileStream(safePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            return await ParseFromExcelStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        // Plain text formats: .csv, .tsv, .txt
        string text = await File.ReadAllTextAsync(safePath, cancellationToken).ConfigureAwait(false);
        return ParseLinesFromText(text, sanitize: true);
    }

    /// <summary>
    /// Parses quick-add / bulk-add text input which may contain single or multiple comma-, semicolon-,
    /// or newline-separated folder names.
    /// </summary>
    public static IReadOnlyList<string> ParseBulkAddInput(string? rawInput, bool asSubfolder = false)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return [];
        }

        var entries = rawInput.Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<string>(entries.Length);
        string prefix = asSubfolder ? "  " : string.Empty;

        foreach (var entry in entries)
        {
            string sanitized = SanitizePathPreservingSeparators(entry);
            if (!string.IsNullOrWhiteSpace(sanitized))
            {
                result.Add(prefix + sanitized);
            }
        }

        return result;
    }

    /// <summary>
    /// Applies a prefix and/or suffix to every line in the raw input text while preserving indentation.
    /// </summary>
    public static string ApplyPrefixSuffix(string? rawInput, string? prefix, string? suffix)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return rawInput ?? string.Empty;
        }

        string p = prefix ?? string.Empty;
        string s = suffix ?? string.Empty;
        if (string.IsNullOrEmpty(p) && string.IsNullOrEmpty(s))
        {
            return rawInput;
        }

        var lines = rawInput.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder(rawInput.Length + lines.Length * (p.Length + s.Length));

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                if (i < lines.Length - 1) sb.AppendLine();
                continue;
            }

            int leadingSpaces = 0;
            while (leadingSpaces < line.Length && (line[leadingSpaces] == ' ' || line[leadingSpaces] == '\t'))
            {
                leadingSpaces++;
            }

            string indent = line[..leadingSpaces];
            string content = line[leadingSpaces..];
            string modified = $"{indent}{p}{content}{s}";

            if (i < lines.Length - 1)
            {
                sb.AppendLine(modified);
            }
            else
            {
                sb.Append(modified);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns predefined hierarchical templates for various workflows.
    /// </summary>
    public static string GetPresetTemplate(string? templateType)
    {
        return (templateType?.ToLowerInvariant() ?? "project") switch
        {
            "project" => """
                Project
                  src
                    components
                    services
                    utils
                  tests
                  docs
                    api
                  assets
                """,
            "archive" => """
                Archive_{date:yyyy-MM-dd}
                  Exports_{time:HHmmss}
                  Backups
                  Logs_{seq:3}
                """,
            "dataset" => """
                Dataset_{seq:3}
                  Images
                  Annotations
                  Labels
                Dataset_{seq:3}
                  Images
                  Annotations
                  Labels
                """,
            "media" => """
                Media_Project_{date:yyyy-MM-dd}
                  01_Footage
                    Raw
                    B-Roll
                  02_Audio
                    Voiceover
                    Music
                    SFX
                  03_Graphics
                  04_Exports
                    Drafts
                    Final
                  05_Documents
                """,
            "web" => """
                WebApp
                  client
                    public
                    src
                      assets
                      components
                      hooks
                      pages
                  server
                    api
                    controllers
                    models
                    middleware
                  database
                    migrations
                  config
                """,
            "office" => """
                Company_{date:yyyy}
                  Accounting
                    Invoices
                    Receipts
                    Tax
                  Human_Resources
                    Contracts
                    Onboarding
                  Marketing
                    Campaigns
                    Designs
                  Operations
                    Reports
                """,
            _ => """
                Folder1
                  SubFolderA
                  SubFolderB
                Folder2
                """
        };
    }

    /// <summary>
    /// Sanitizes an entire path preserving forward or backward slashes between segments.
    /// </summary>
    public static string SanitizePathPreservingSeparators(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return string.Empty;
        }

        var segments = rawPath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return string.Empty;
        }

        var sanitizedSegments = segments.Select(s => SanitizeFolderName(s));
        return string.Join("/", sanitizedSegments);
    }

    private static bool IsLikelyHeaderRow(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string lower = text.Trim().ToLowerInvariant();

        return lower is "folder" or "folder name" or "folders" or "directory" or "path"
            or "name" or "thư mục" or "tên thư mục" or "đường dẫn" or "tên" or "stt"
            or "danh sách" or "list" or "item" or "items" or "title"
            || lower.StartsWith("folder,", StringComparison.Ordinal)
            || lower.StartsWith("folder name,", StringComparison.Ordinal)
            || lower.StartsWith("thư mục,", StringComparison.Ordinal)
            || lower.StartsWith("folder\t", StringComparison.Ordinal)
            || lower.StartsWith("thư mục\t", StringComparison.Ordinal);
    }

    private static int GetColumnIndex(string cellRef)
    {
        if (string.IsNullOrEmpty(cellRef)) return 0;
        int col = 0;
        foreach (char ch in cellRef)
        {
            if (char.IsLetter(ch))
            {
                col = (col * 26) + (char.ToUpperInvariant(ch) - 'A' + 1);
            }
            else break;
        }
        return Math.Max(0, col - 1);
    }

    private static string GetCellValue(XElement cell, List<string> sharedStrings, XNamespace ns)
    {
        string? type = cell.Attribute("t")?.Value;
        if (type == "s")
        {
            string? val = cell.Element(ns + "v")?.Value;
            if (int.TryParse(val, out int idx) && idx >= 0 && idx < sharedStrings.Count)
            {
                return sharedStrings[idx];
            }
            return string.Empty;
        }

        if (type == "inlineStr")
        {
            var isElem = cell.Element(ns + "is");
            if (isElem is not null)
            {
                return string.Concat(isElem.Descendants(ns + "t").Select(t => t.Value));
            }
            return string.Empty;
        }

        return cell.Element(ns + "v")?.Value ?? string.Empty;
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

    /// <summary>
    /// Parses a raw line to determine its visual indentation column and extracts the clean folder name
    /// by stripping ASCII/Unicode tree branch markers (├──, └──, │, |), markdown bullets (- , * , • ),
    /// and folder emoji prefixes (📁, 📂).
    /// </summary>
    public static (int IndentColumn, string CleanName) ExtractTreeLine(string rawLine)
    {
        if (string.IsNullOrEmpty(rawLine))
        {
            return (0, string.Empty);
        }

        int column = 0;
        int i = 0;
        int len = rawLine.Length;

        while (i < len)
        {
            char c = rawLine[i];

            if (c == ' ')
            {
                column++;
                i++;
            }
            else if (c == '\t')
            {
                column = (column / 4 + 1) * 4;
                i++;
            }
            else if (c == '│' || c == '|')
            {
                // Vertical branch line - advance column
                column += 4;
                i++;
                // Skip trailing spaces after branch pipe
                while (i < len && rawLine[i] == ' ')
                {
                    i++;
                }
            }
            else if (StartsWithBranchSymbol(rawLine, i, out int branchLen))
            {
                column += 4;
                i += branchLen;
                while (i < len && (rawLine[i] == ' ' || rawLine[i] == '\t'))
                {
                    i++;
                }
            }
            else if (StartsWithBullet(rawLine, i, out int bulletLen))
            {
                i += bulletLen;
                while (i < len && (rawLine[i] == ' ' || rawLine[i] == '\t'))
                {
                    i++;
                }
                break;
            }
            else
            {
                break;
            }
        }

        string remaining = i < len ? rawLine[i..].Trim() : string.Empty;

        // Strip folder emoji prefix if present (📁, 📂)
        if (remaining.StartsWith("📁", StringComparison.Ordinal) || remaining.StartsWith("📂", StringComparison.Ordinal))
        {
            remaining = remaining[2..].TrimStart(); // emoji is surrogate pair (2 chars)
        }

        // Trim trailing slashes if present
        remaining = remaining.TrimEnd('/', '\\');

        return (column, remaining);
    }

    private static bool StartsWithBranchSymbol(string text, int index, out int length)
    {
        string[] symbols = ["├──", "└──", "+--", "\\--", "├--", "└--", "├", "└"];
        foreach (var s in symbols)
        {
            if (index + s.Length <= text.Length && text.AsSpan(index, s.Length).SequenceEqual(s))
            {
                length = s.Length;
                return true;
            }
        }
        length = 0;
        return false;
    }

    private static bool StartsWithBullet(string text, int index, out int length)
    {
        if (index < text.Length)
        {
            char c = text[index];
            if (c == '-' || c == '*' || c == '•')
            {
                if (index + 1 == text.Length || text[index + 1] == ' ')
                {
                    length = (index + 1 < text.Length && text[index + 1] == ' ') ? 2 : 1;
                    return true;
                }
            }
        }
        length = 0;
        return false;
    }

    /// <summary>
    /// Generates a clean, monospaced ASCII/Unicode directory tree diagram (similar to the 'tree' command)
    /// representing the exact folder hierarchy with optional status tags.
    /// </summary>
    public static string GenerateTreeDiagram(
        string? rootName,
        IReadOnlyList<FolderItemNode> rootNodes,
        bool includeRoot = true,
        bool showStatusBadges = false)
    {
        if (rootNodes is null || rootNodes.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        string displayRoot = string.IsNullOrWhiteSpace(rootName) ? "Root" : rootName.Trim();

        if (includeRoot)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"📁 {displayRoot}/");
            AppendTreeChildren(sb, rootNodes, prefix: "", showStatusBadges);
        }
        else
        {
            for (int i = 0; i < rootNodes.Count; i++)
            {
                bool isLast = i == rootNodes.Count - 1;
                AppendTreeNode(sb, rootNodes[i], prefix: "", isLast, showStatusBadges);
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendTreeChildren(
        StringBuilder sb,
        IReadOnlyList<FolderItemNode> children,
        string prefix,
        bool showStatusBadges)
    {
        for (int i = 0; i < children.Count; i++)
        {
            bool isLast = i == children.Count - 1;
            AppendTreeNode(sb, children[i], prefix, isLast, showStatusBadges);
        }
    }

    private static void AppendTreeNode(
        StringBuilder sb,
        FolderItemNode node,
        string prefix,
        bool isLast,
        bool showStatusBadges)
    {
        string branch = isLast ? "└── " : "├── ";
        string statusText = string.Empty;

        if (showStatusBadges)
        {
            statusText = node.Status switch
            {
                FolderItemStatus.PlannedNew => " [New]",
                FolderItemStatus.SkippedExisting => " [Exists]",
                FolderItemStatus.WillOverwrite => " [Overwrite]",
                FolderItemStatus.SuffixAppended => " [Renamed]",
                FolderItemStatus.Failed => " [Failed]",
                FolderItemStatus.Invalid => " [Invalid]",
                _ => ""
            };
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{branch}📁 {node.EvaluatedName}{statusText}");

        if (node.Children.Count > 0)
        {
            string childPrefix = prefix + (isLast ? "    " : "│   ");
            AppendTreeChildren(sb, node.Children, childPrefix, showStatusBadges);
        }
    }
}
