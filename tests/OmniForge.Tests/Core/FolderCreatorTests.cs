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
}
