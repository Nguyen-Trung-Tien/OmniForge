using OmniForge.Core.Contracts;
using OmniForge.Core.Models;
using OmniForge.Modules.BatchOps;

namespace OmniForge.Tests.Core;

public sealed class BatchRenameEngineTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly BatchRenameEngine _engine;

    public BatchRenameEngineTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OmniForge_Rename_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _engine = new BatchRenameEngine();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private string CreateTestFile(string relativeName, string content = "test content")
    {
        string fullPath = Path.Combine(_tempDirectory, relativeName);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    [Fact]
    public void BuildPreview_ChainedRules_ShouldApplySequentially()
    {
        var file1 = CreateTestFile("hello world.txt");
        var meta1 = FileMetadata.FromPath(file1);

        var rules = new IRenameRule[]
        {
            new ChangeCaseRule(CaseMode.TitleCase),
            new ReplaceRule(" ", "_", matchCase: false, useRegex: false),
            new PrefixSuffixRule("PRE_", "_POST")
        };

        var result = _engine.BuildPreview([meta1], rules);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        var preview = result.Value[0];
        Assert.Equal("PRE_Hello_World_POST.txt", preview.ProposedFileName);
        Assert.False(preview.HasConflict);
        Assert.True(preview.HasChange);
        Assert.Equal(RenameItemStatus.Pending, preview.Status);
    }

    [Fact]
    public void BuildPreview_InvalidFileNameCharacters_ShouldFlagConflict()
    {
        var file1 = CreateTestFile("file1.txt");
        var meta1 = FileMetadata.FromPath(file1);

        var rules = new IRenameRule[]
        {
            new PrefixSuffixRule("bad:name?", "")
        };

        var result = _engine.BuildPreview([meta1], rules);

        Assert.True(result.IsSuccess);
        var preview = result.Value[0];
        Assert.True(preview.HasConflict);
        Assert.Equal(RenameItemStatus.Conflict, preview.Status);
        Assert.Contains("invalid characters", preview.ValidationError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildPreview_ReservedWindowsDeviceName_ShouldFlagConflict()
    {
        var file1 = CreateTestFile("file1.txt");
        var meta1 = FileMetadata.FromPath(file1);

        var rules = new IRenameRule[]
        {
            new ReplaceRule("file1", "CON", matchCase: false, useRegex: false)
        };

        var result = _engine.BuildPreview([meta1], rules);

        Assert.True(result.IsSuccess);
        var preview = result.Value[0];
        Assert.True(preview.HasConflict);
        Assert.Contains("reserved Windows device name", preview.ValidationError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildPreview_IntraBatchDuplicate_ShouldFlagCollision()
    {
        var file1 = CreateTestFile("alpha.txt");
        var file2 = CreateTestFile("beta.txt");
        var meta1 = FileMetadata.FromPath(file1);
        var meta2 = FileMetadata.FromPath(file2);

        // Rule renames both alpha and beta to "same"
        var rules = new IRenameRule[]
        {
            new ReplaceRule(".*", "same", matchCase: false, useRegex: true)
        };

        var result = _engine.BuildPreview([meta1, meta2], rules);

        Assert.True(result.IsSuccess);
        Assert.All(result.Value, item =>
        {
            Assert.True(item.HasConflict);
            Assert.Contains("Collision", item.ValidationError);
        });
    }

    [Fact]
    public void BuildPreview_TargetFileAlreadyExistsOnDisk_ShouldFlagConflict()
    {
        var file1 = CreateTestFile("source.txt");
        var existing = CreateTestFile("existing.txt"); // Target file exists!
        var meta1 = FileMetadata.FromPath(file1);

        var rules = new IRenameRule[]
        {
            new ReplaceRule("source", "existing", matchCase: false, useRegex: false)
        };

        var result = _engine.BuildPreview([meta1], rules);

        Assert.True(result.IsSuccess);
        var preview = result.Value[0];
        Assert.True(preview.HasConflict);
        Assert.Contains("already exists on disk", preview.ValidationError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteBatchAsync_ShouldRenameFilesAndReturnJournal()
    {
        var file1 = CreateTestFile("doc1.txt");
        var file2 = CreateTestFile("doc2.txt");

        var meta1 = FileMetadata.FromPath(file1);
        var meta2 = FileMetadata.FromPath(file2);

        var rules = new IRenameRule[]
        {
            new PrefixSuffixRule("ARCHIVE_", "")
        };

        var preview = _engine.BuildPreview([meta1, meta2], rules).Value;

        var progressReports = new List<BatchProgressReport>();
        var progress = new Progress<BatchProgressReport>(p => progressReports.Add(p));

        var execResult = await _engine.ExecuteBatchAsync(preview, progress);

        Assert.True(execResult.IsSuccess);
        var journal = execResult.Value;
        Assert.Equal(2, journal.CompletedOperations.Count);

        string expected1 = Path.Combine(_tempDirectory, "ARCHIVE_doc1.txt");
        string expected2 = Path.Combine(_tempDirectory, "ARCHIVE_doc2.txt");

        Assert.True(File.Exists(expected1));
        Assert.True(File.Exists(expected2));
        Assert.False(File.Exists(file1));
        Assert.False(File.Exists(file2));

        // Test Undo rollback
        var undoResult = await _engine.UndoBatchAsync(journal);
        Assert.True(undoResult.IsSuccess);
        Assert.Equal(2, undoResult.Value);

        Assert.True(File.Exists(file1));
        Assert.True(File.Exists(file2));
        Assert.False(File.Exists(expected1));
        Assert.False(File.Exists(expected2));
    }

    [Fact]
    public async Task ExecuteBatchAsync_WhenConflictPresent_ShouldRefuseExecution()
    {
        var file1 = CreateTestFile("doc.txt");
        var meta = FileMetadata.FromPath(file1);

        var conflictItem = new RenamePreviewItem(
            SourceFullPath: file1,
            OriginalFileName: "doc.txt",
            ProposedFileName: "doc?.txt",
            TargetFullPath: Path.Combine(_tempDirectory, "doc?.txt"),
            Extension: ".txt",
            SizeBytes: 100,
            HasChange: true,
            HasConflict: true,
            Status: RenameItemStatus.Conflict,
            ValidationError: "Invalid character.");

        var result = await _engine.ExecuteBatchAsync([conflictItem]);

        Assert.True(result.IsFailure);
        Assert.Contains("collision", result.Error.Code, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteStreamingAsync_ShouldStreamOutcomes()
    {
        var file1 = CreateTestFile("stream1.txt");
        var meta1 = FileMetadata.FromPath(file1);

        var rules = new IRenameRule[]
        {
            new PrefixSuffixRule("STREAM_", "")
        };

        var preview = _engine.BuildPreview([meta1], rules).Value;

        var results = new List<RenameItemResult>();
        await foreach (var item in _engine.ExecuteStreamingAsync(preview))
        {
            results.Add(item);
        }

        Assert.Single(results);
        Assert.True(results[0].IsSuccess);
        Assert.True(File.Exists(Path.Combine(_tempDirectory, "STREAM_stream1.txt")));
    }

    [Fact]
    public async Task BuildPreviewAndExecute_CaseOnlyRename_ShouldDetectChangeWithoutDiskCollision()
    {
        var file1 = CreateTestFile("photo_sample.txt");
        var meta1 = FileMetadata.FromPath(file1);

        var rules = new IRenameRule[]
        {
            new ChangeCaseRule(CaseMode.Uppercase)
        };

        var previewResult = _engine.BuildPreview([meta1], rules);

        Assert.True(previewResult.IsSuccess);
        var item = Assert.Single(previewResult.Value);
        Assert.Equal("PHOTO_SAMPLE.txt", item.ProposedFileName);
        Assert.True(item.HasChange);
        Assert.False(item.HasConflict);
        Assert.Equal(RenameItemStatus.Pending, item.Status);

        var execResult = await _engine.ExecuteBatchAsync(previewResult.Value);
        Assert.True(execResult.IsSuccess);
        Assert.Single(execResult.Value.CompletedOperations);
    }
}

