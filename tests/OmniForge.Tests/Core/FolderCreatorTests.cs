using OmniForge.Core.Contracts;
using OmniForge.Core.Modules.FolderCreator;

namespace OmniForge.Tests.Core;

public sealed class FolderCreatorTests : IDisposable
{
    private readonly string _tempTestDir;

    public FolderCreatorTests()
    {
        _tempTestDir = Path.Combine(Path.GetTempPath(), "OmniForge_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempTestDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempTestDir))
            {
                Directory.Delete(_tempTestDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void TokenEvaluator_EvaluatesDateAndCustomFormat()
    {
        var fixedDate = new DateTime(2026, 11, 1, 14, 30, 0);

        string defaultDate = TokenEvaluator.Evaluate("Logs_{date}", 1, fixedDate);
        Assert.Equal("Logs_2026-11-01", defaultDate);

        string customDate = TokenEvaluator.Evaluate("Archive_{date:yyyyMMdd}", 1, fixedDate);
        Assert.Equal("Archive_20261101", customDate);
    }

    [Fact]
    public void TokenEvaluator_EvaluatesTimeAndCustomFormat()
    {
        var fixedDate = new DateTime(2026, 11, 1, 14, 30, 45);

        string defaultTime = TokenEvaluator.Evaluate("Dump_{time}", 1, fixedDate);
        Assert.Equal("Dump_143045", defaultTime);

        string customTime = TokenEvaluator.Evaluate("Snapshot_{time:HH-mm-ss}", 1, fixedDate);
        Assert.Equal("Snapshot_14-30-45", customTime);
    }

    [Fact]
    public void TokenEvaluator_EvaluatesGuid()
    {
        string evaluated = TokenEvaluator.Evaluate("Session_{guid}", 1);
        Assert.StartsWith("Session_", evaluated);
        string guidPart = evaluated["Session_".Length..];
        Assert.True(Guid.TryParse(guidPart, out _));
    }

    [Fact]
    public void TokenEvaluator_EvaluatesSequenceWithPaddingAndStart()
    {
        string seq3 = TokenEvaluator.Evaluate("Batch_{seq:3}", 5);
        Assert.Equal("Batch_005", seq3);

        string seq2 = TokenEvaluator.Evaluate("Item_{seq:2}", 9);
        Assert.Equal("Item_09", seq2);

        string seqDefault = TokenEvaluator.Evaluate("Item_{seq}", 42);
        Assert.Equal("Item_42", seqDefault);

        string seqWithStart = TokenEvaluator.Evaluate("Item_{seq:3,10}", 1);
        Assert.Equal("Item_010", seqWithStart);
    }

    [Fact]
    public void TokenEvaluator_HandlesInvalidFormatsGracefully()
    {
        var fixedDate = new DateTime(2026, 11, 1);
        // Unclosed quote in format string causes FormatException in .NET
        string result = TokenEvaluator.Evaluate("Test_{date:'unclosed}", 1, fixedDate);
        Assert.Equal("Test_2026-11-01", result);
    }

    [Fact]
    public void PathSanitizer_ReplacesIllegalCharacters()
    {
        string input = "Report: 2026*final? <draft> | test \"quotes\" / slash \\ back";
        string sanitized = FolderParser.SanitizeFolderName(input);

        Assert.DoesNotContain(':', sanitized);
        Assert.DoesNotContain('*', sanitized);
        Assert.DoesNotContain('?', sanitized);
        Assert.DoesNotContain('<', sanitized);
        Assert.DoesNotContain('>', sanitized);
        Assert.DoesNotContain('|', sanitized);
        Assert.DoesNotContain('"', sanitized);
        Assert.DoesNotContain('/', sanitized);
        Assert.DoesNotContain('\\', sanitized);
    }

    [Fact]
    public void PathSanitizer_TrimsTrailingDotsAndSpaces()
    {
        string input = "Folder Name...   ";
        string sanitized = FolderParser.SanitizeFolderName(input);
        Assert.Equal("Folder Name", sanitized);
    }

    [Fact]
    public void PathSanitizer_EscapesReservedWindowsDeviceNames()
    {
        Assert.Equal("_CON", FolderParser.SanitizeFolderName("CON"));
        Assert.Equal("_PRN", FolderParser.SanitizeFolderName("prn"));
        Assert.Equal("_AUX", FolderParser.SanitizeFolderName("AUX"));
        Assert.Equal("_NUL", FolderParser.SanitizeFolderName("nul"));
        Assert.Equal("_COM1", FolderParser.SanitizeFolderName("COM1"));
        Assert.Equal("_LPT9", FolderParser.SanitizeFolderName("lpt9"));
    }

    [Fact]
    public void FolderParser_ParsesTreeHierarchy_WithTwoSpacesIndentation()
    {
        string template = """
            Project
              src
                components
                utils
              docs
                api
            """;

        var result = FolderParser.Parse(template, FolderInputMode.TreeHierarchy);
        Assert.True(result.IsSuccess);

        var roots = result.Value;
        Assert.Single(roots);
        var project = roots[0];
        Assert.Equal("Project", project.EvaluatedName);
        Assert.Equal(2, project.Children.Count);

        var src = project.Children[0];
        Assert.Equal("src", src.EvaluatedName);
        Assert.Equal(2, src.Children.Count);
        Assert.Equal("components", src.Children[0].EvaluatedName);
        Assert.Equal("utils", src.Children[1].EvaluatedName);

        var docs = project.Children[1];
        Assert.Equal("docs", docs.EvaluatedName);
        Assert.Single(docs.Children);
        Assert.Equal("api", docs.Children[0].EvaluatedName);
    }

    [Fact]
    public void FolderParser_ParsesTreeHierarchy_WithFourSpacesAndTabs()
    {
        string template = "Root\n    FolderA\n        SubA\n\tFolderB\n\t\tSubB";
        var result = FolderParser.Parse(template, FolderInputMode.TreeHierarchy);
        Assert.True(result.IsSuccess);

        var roots = result.Value;
        Assert.Single(roots);
        var root = roots[0];
        Assert.Equal("Root", root.EvaluatedName);
        Assert.Equal(2, root.Children.Count);
        Assert.Equal("FolderA", root.Children[0].EvaluatedName);
        Assert.Equal("FolderB", root.Children[1].EvaluatedName);
        Assert.Equal("SubA", root.Children[0].Children[0].EvaluatedName);
        Assert.Equal("SubB", root.Children[1].Children[0].EvaluatedName);
    }

    [Fact]
    public void FolderParser_ParsesFlatList()
    {
        string template = """
            FolderA
            FolderB
            Nested/SubFolder
            """;

        var result = FolderParser.Parse(template, FolderInputMode.Flat);
        Assert.True(result.IsSuccess);

        var roots = result.Value;
        Assert.Equal(3, roots.Count);
        Assert.Equal("FolderA", roots[0].EvaluatedName);
        Assert.Equal("FolderB", roots[1].EvaluatedName);
        Assert.Equal("Nested", roots[2].EvaluatedName);
        Assert.Single(roots[2].Children);
        Assert.Equal("SubFolder", roots[2].Children[0].EvaluatedName);
    }

    [Fact]
    public void FolderParser_IncrementsSequenceNumber()
    {
        string template = """
            Module_{seq:2}
            Module_{seq:2}
            Module_{seq:2}
            """;

        var result = FolderParser.Parse(template, FolderInputMode.Flat, startSequence: 1);
        Assert.True(result.IsSuccess);

        var roots = result.Value;
        Assert.Equal(3, roots.Count);
        Assert.Equal("Module_01", roots[0].EvaluatedName);
        Assert.Equal("Module_02", roots[1].EvaluatedName);
        Assert.Equal("Module_03", roots[2].EvaluatedName);
    }

    [Fact]
    public void FolderCreatorEngine_DryRun_DetectsNewAndExistingFolders()
    {
        // Pre-create an existing folder
        string existingPath = Path.Combine(_tempTestDir, "ExistingFolder");
        Directory.CreateDirectory(existingPath);

        string template = """
            ExistingFolder
            NewFolder
            """;

        var engine = new FolderCreatorEngine();
        var dryRun = engine.InspectDryRun(_tempTestDir, template, FolderInputMode.Flat, FolderConflictResolution.SkipExisting);

        Assert.True(dryRun.IsSuccess);
        Assert.Equal(2, dryRun.Value.TotalCount);
        Assert.Equal(1, dryRun.Value.NewCount);
        Assert.Equal(1, dryRun.Value.ExistingCount);

        var existingNode = dryRun.Value.AllItems.First(x => x.EvaluatedName == "ExistingFolder");
        Assert.Equal(FolderItemStatus.SkippedExisting, existingNode.Status);

        var newNode = dryRun.Value.AllItems.First(x => x.EvaluatedName == "NewFolder");
        Assert.Equal(FolderItemStatus.PlannedNew, newNode.Status);
    }

    [Fact]
    public void FolderCreatorEngine_DryRun_AppendsSuffixOnConflict()
    {
        string existingPath = Path.Combine(_tempTestDir, "Photos");
        Directory.CreateDirectory(existingPath);

        string template = "Photos";
        var engine = new FolderCreatorEngine();
        var dryRun = engine.InspectDryRun(_tempTestDir, template, FolderInputMode.Flat, FolderConflictResolution.AppendSuffix);

        Assert.True(dryRun.IsSuccess);
        var item = dryRun.Value.AllItems[0];
        Assert.Equal("Photos (1)", item.EvaluatedName);
        Assert.Equal(FolderItemStatus.SuffixAppended, item.Status);
    }

    [Fact]
    public void FolderCreatorEngine_DryRun_HandlesOverwriteConflict()
    {
        string existingPath = Path.Combine(_tempTestDir, "SharedDocs");
        Directory.CreateDirectory(existingPath);

        string template = "SharedDocs";
        var engine = new FolderCreatorEngine();
        var dryRun = engine.InspectDryRun(_tempTestDir, template, FolderInputMode.Flat, FolderConflictResolution.Overwrite);

        Assert.True(dryRun.IsSuccess);
        var item = dryRun.Value.AllItems[0];
        Assert.Equal("SharedDocs", item.EvaluatedName);
        Assert.Equal(FolderItemStatus.WillOverwrite, item.Status);
    }

    [Fact]
    public void PathSanitizer_NeutralizesPathTraversalDots()
    {
        // Path traversal attempts with .. are stripped and neutralized to safe fallback
        string sanitized = FolderParser.SanitizeFolderName("..");
        Assert.Equal("Folder", sanitized);

        string sanitizedWithSlashes = FolderParser.SanitizeFolderName("../../etc/passwd");
        Assert.DoesNotContain('/', sanitizedWithSlashes);
        Assert.DoesNotContain('\\', sanitizedWithSlashes);
    }

    [Fact]
    public void FolderParser_ParsesSequenceToken_WithCustomStartAcrossLines()
    {
        string template = """
            Release_{seq:3,10}
            Release_{seq:3,10}
            Release_{seq:3,10}
            """;

        var result = FolderParser.Parse(template, FolderInputMode.Flat, startSequence: 1);
        Assert.True(result.IsSuccess);
        Assert.Equal("Release_010", result.Value[0].EvaluatedName);
        Assert.Equal("Release_011", result.Value[1].EvaluatedName);
        Assert.Equal("Release_012", result.Value[2].EvaluatedName);
    }

    [Fact]
    public async Task FolderCreatorEngine_SupportsCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled

        var engine = new FolderCreatorEngine();
        var items = new List<FolderItemNode>
        {
            new() { EvaluatedName = "CancelledFolder", FullPath = Path.Combine(_tempTestDir, "CancelledFolder") }
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await engine.ExecuteCreationAsync(items, cancellationToken: cts.Token);
        });
    }

    [Fact]
    public async Task FolderCreatorEngine_ExecutesCreation_WithFaultIsolation()
    {
        string template = """
            Project
              src
              docs
            """;

        var engine = new FolderCreatorEngine();
        var dryRun = engine.InspectDryRun(_tempTestDir, template, FolderInputMode.TreeHierarchy);
        Assert.True(dryRun.IsSuccess);

        var result = await engine.ExecuteCreationAsync(dryRun.Value.AllItems);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.CreatedCount);
        Assert.Equal(0, result.Value.FailedCount);

        Assert.True(Directory.Exists(Path.Combine(_tempTestDir, "Project", "src")));
        Assert.True(Directory.Exists(Path.Combine(_tempTestDir, "Project", "docs")));
    }

    [Fact]
    public void FolderParser_GenerateSeriesText_CreatesSequentialFolderRows()
    {
        string generated = FolderParser.GenerateSeriesText("Lesson_{seq:2}", count: 3, startSequence: 5);
        var lines = generated.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length);
        Assert.Equal("Lesson_05", lines[0]);
        Assert.Equal("Lesson_06", lines[1]);
        Assert.Equal("Lesson_07", lines[2]);
    }

    [Fact]
    public void FolderParser_GenerateSeriesText_AutoAppendsSequenceTokenWhenMissing()
    {
        string generated = FolderParser.GenerateSeriesText("Chapter", count: 2, startSequence: 1, appendToExisting: true, existingText: "Root");
        var lines = generated.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lines.Length);
        Assert.Equal("Root", lines[0]);
        Assert.Equal("Chapter_01", lines[1]);
        Assert.Equal("Chapter_02", lines[2]);
    }

    [Fact]
    public void FolderParser_AdjustIndentation_IndentsAndOutdentsSafely()
    {
        string raw = "Parent\n  Child";
        string indented = FolderParser.AdjustIndentation(raw, 2);
        Assert.Equal("  Parent\n    Child", indented.Replace("\r\n", "\n"));

        string outdented = FolderParser.AdjustIndentation(indented, -2);
        Assert.Equal("Parent\n  Child", outdented.Replace("\r\n", "\n"));

        // Outdenting past 0 clamps at 0
        string clamped = FolderParser.AdjustIndentation("Root", -4);
        Assert.Equal("Root", clamped);
    }

    [Fact]
    public void FolderParser_NormalizeLines_CleansAndSortsLines()
    {
        string messy = "  Zebra  \n\n  Alpha \r\n\r\n  Beta  ";
        string cleaned = FolderParser.NormalizeLines(messy, sortAlphabetically: false);
        Assert.Equal($"  Zebra{Environment.NewLine}  Alpha{Environment.NewLine}  Beta", cleaned);

        string sorted = FolderParser.NormalizeLines(messy, sortAlphabetically: true);
        Assert.Equal($"  Alpha{Environment.NewLine}  Beta{Environment.NewLine}  Zebra", sorted);
    }

    [Fact]
    public void FolderParser_ParseLinesFromText_ParsesDelimitedAndSingleLines()
    {
        string raw = "Folder Alpha, Folder Beta, Folder Gamma";
        var result = FolderParser.ParseLinesFromText(raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal("Folder Alpha", result.Value[0]);
        Assert.Equal("Folder Beta", result.Value[1]);
        Assert.Equal("Folder Gamma", result.Value[2]);
    }

    [Fact]
    public void FolderParser_ParseLinesFromText_SkipsHeadersAndHandlesCsv()
    {
        string csv = """
            Folder Name, Status
            1, Client_A
            2, Client_B
            3, Client_C
            """;
        var result = FolderParser.ParseLinesFromText(csv);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal("Client_A", result.Value[0]);
        Assert.Equal("Client_B", result.Value[1]);
        Assert.Equal("Client_C", result.Value[2]);
    }

    [Fact]
    public void FolderParser_ParseBulkAddInput_SplitsMultipleSeparatorsAndHandlesSubfolders()
    {
        string input = "Alpha, Beta; Gamma\nDelta";
        var topLevel = FolderParser.ParseBulkAddInput(input, asSubfolder: false);
        Assert.Equal(4, topLevel.Count);
        Assert.Equal("Alpha", topLevel[0]);
        Assert.Equal("Beta", topLevel[1]);
        Assert.Equal("Gamma", topLevel[2]);
        Assert.Equal("Delta", topLevel[3]);

        var subfolders = FolderParser.ParseBulkAddInput(input, asSubfolder: true);
        Assert.Equal(4, subfolders.Count);
        Assert.Equal("  Alpha", subfolders[0]);
        Assert.Equal("  Beta", subfolders[1]);
        Assert.Equal("  Gamma", subfolders[2]);
        Assert.Equal("  Delta", subfolders[3]);
    }

    [Fact]
    public void FolderParser_ApplyPrefixSuffix_AppliesToLinesPreservingIndentation()
    {
        string raw = "Root\n  Child1\n    GrandChild";
        string modified = FolderParser.ApplyPrefixSuffix(raw, prefix: "2026_", suffix: "_v1");

        var lines = modified.Replace("\r\n", "\n").Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Equal("2026_Root_v1", lines[0]);
        Assert.Equal("  2026_Child1_v1", lines[1]);
        Assert.Equal("    2026_GrandChild_v1", lines[2]);
    }

    [Fact]
    public void FolderParser_GetPresetTemplate_ReturnsAllTemplateTypes()
    {
        string project = FolderParser.GetPresetTemplate("project");
        Assert.Contains("src", project);
        Assert.Contains("components", project);

        string web = FolderParser.GetPresetTemplate("web");
        Assert.Contains("client", web);
        Assert.Contains("server", web);

        string media = FolderParser.GetPresetTemplate("media");
        Assert.Contains("01_Footage", media);
        Assert.Contains("02_Audio", media);

        string archive = FolderParser.GetPresetTemplate("archive");
        Assert.Contains("Archive_{date:yyyy-MM-dd}", archive);

        string dataset = FolderParser.GetPresetTemplate("dataset");
        Assert.Contains("Dataset_{seq:3}", dataset);

        string office = FolderParser.GetPresetTemplate("office");
        Assert.Contains("Accounting", office);
    }

    [Fact]
    public async Task FolderParser_ParseFromExcelStreamAsync_ParsesWorksheetAndSkipsHeaders()
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var ssEntry = archive.CreateEntry("xl/sharedStrings.xml");
            using (var writer = new StreamWriter(ssEntry.Open()))
            {
                writer.Write("""
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" count="4" uniqueCount="4">
                        <si><t>Folder Name</t></si>
                        <si><t>Department_HR</t></si>
                        <si><t>Department_IT</t></si>
                        <si><t>Department_Finance</t></si>
                    </sst>
                    """);
            }

            var sheetEntry = archive.CreateEntry("xl/worksheets/sheet1.xml");
            using (var writer = new StreamWriter(sheetEntry.Open()))
            {
                writer.Write("""
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                        <sheetData>
                            <row r="1">
                                <c r="A1" t="s"><v>0</v></c>
                            </row>
                            <row r="2">
                                <c r="A2" t="s"><v>1</v></c>
                            </row>
                            <row r="3">
                                <c r="A3" t="s"><v>2</v></c>
                            </row>
                            <row r="4">
                                <c r="A4" t="s"><v>3</v></c>
                            </row>
                        </sheetData>
                    </worksheet>
                    """);
            }
        }

        memoryStream.Position = 0;
        var result = await FolderParser.ParseFromExcelStreamAsync(memoryStream);

        Assert.True(result.IsSuccess);
        var items = result.Value;
        Assert.Equal(3, items.Count);
        Assert.Equal("Department_HR", items[0]);
        Assert.Equal("Department_IT", items[1]);
        Assert.Equal("Department_Finance", items[2]);
    }

    [Fact]
    public async Task FolderParser_ImportFromFileAsync_ReadsTextAndCsvFiles()
    {
        string testFilePath = Path.Combine(_tempTestDir, "folders.txt");
        await File.WriteAllTextAsync(testFilePath, "Folder_Alpha\nFolder_Beta/Sub1\nFolder_Gamma");

        var result = await FolderParser.ImportFromFileAsync(testFilePath);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal("Folder_Alpha", result.Value[0]);
        Assert.Equal("Folder_Beta/Sub1", result.Value[1]);
        Assert.Equal("Folder_Gamma", result.Value[2]);
    }

    [Fact]
    public void FolderParser_ParseTreeHierarchy_HandlesAsciiTreeDiagram()
    {
        string input = """
            Project
            ├── src
            │   ├── components
            │   └── services
            └── docs
            """;
        var result = FolderParser.Parse(input, FolderInputMode.TreeHierarchy);
        Assert.True(result.IsSuccess);
        var roots = result.Value;
        Assert.Single(roots);
        var project = roots[0];
        Assert.Equal("Project", project.EvaluatedName);
        Assert.Equal(2, project.Children.Count);
        Assert.Equal("src", project.Children[0].EvaluatedName);
        Assert.Equal("docs", project.Children[1].EvaluatedName);
        Assert.Equal(2, project.Children[0].Children.Count);
        Assert.Equal("components", project.Children[0].Children[0].EvaluatedName);
        Assert.Equal("services", project.Children[0].Children[1].EvaluatedName);
    }

    [Fact]
    public void FolderParser_ParseTreeHierarchy_HandlesMarkdownBulletList()
    {
        string input = """
            - Project
              - src
                - components
              - docs
            """;
        var result = FolderParser.Parse(input, FolderInputMode.TreeHierarchy);
        Assert.True(result.IsSuccess);
        var roots = result.Value;
        Assert.Single(roots);
        var project = roots[0];
        Assert.Equal("Project", project.EvaluatedName);
        Assert.Equal(2, project.Children.Count);
        Assert.Equal("src", project.Children[0].EvaluatedName);
        Assert.Equal("docs", project.Children[1].EvaluatedName);
        Assert.Single(project.Children[0].Children);
        Assert.Equal("components", project.Children[0].Children[0].EvaluatedName);
    }

    [Fact]
    public void FolderParser_ParseTreeHierarchy_HandlesInlineSlashSubpaths()
    {
        string input = """
            Project
              src/components
              docs
            """;
        var result = FolderParser.Parse(input, FolderInputMode.TreeHierarchy);
        Assert.True(result.IsSuccess);
        var roots = result.Value;
        Assert.Single(roots);
        var project = roots[0];
        Assert.Equal("Project", project.EvaluatedName);
        Assert.Equal(2, project.Children.Count);
        Assert.Equal("src", project.Children[0].EvaluatedName);
        Assert.Single(project.Children[0].Children);
        Assert.Equal("components", project.Children[0].Children[0].EvaluatedName);
    }

    [Fact]
    public void FolderParser_GenerateTreeDiagram_ProducesFormattedTree()
    {
        string input = """
            Project
              src
                components
              docs
            """;
        var parseResult = FolderParser.Parse(input, FolderInputMode.TreeHierarchy);
        Assert.True(parseResult.IsSuccess);
        string diagram = FolderParser.GenerateTreeDiagram("OmniForge", parseResult.Value, includeRoot: true);

        Assert.Contains("📁 OmniForge/", diagram);
        Assert.Contains("└── 📁 Project", diagram);
        Assert.Contains("├── 📁 src", diagram);
        Assert.Contains("└── 📁 components", diagram);
        Assert.Contains("└── 📁 docs", diagram);
    }
}
