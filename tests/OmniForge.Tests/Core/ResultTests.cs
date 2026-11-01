using OmniForge.Core.Common;

namespace OmniForge.Tests.Core;

public sealed class ResultTests
{
    [Fact]
    public void Success_WithoutValue_ShouldBeSuccessAndHaveNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
        Assert.False(result.Error.HasError);
    }

    [Fact]
    public void Failure_WithCodeAndMessage_ShouldBeFailure()
    {
        var result = Result.Failure("Test.Error", "Something went wrong.");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Test.Error", result.Error.Code);
        Assert.Equal("Something went wrong.", result.Error.Message);
    }

    [Fact]
    public void GenericResult_MapAndBind_ShouldTransformOnSuccess()
    {
        var initial = Result.Success(21);

        var mapped = initial.Map(x => x * 2);
        var bound = mapped.Bind(x => Result.Success($"Answer: {x}"));

        Assert.True(bound.IsSuccess);
        Assert.Equal("Answer: 42", bound.Value);
    }

    [Fact]
    public void GenericResult_AccessingValueOnFailure_ShouldThrowInvalidOperationException()
    {
        var failed = Result.Failure<int>("Math.DivideByZero", "Cannot divide by zero.");

        Assert.False(failed.TryGetValue(out _));
        Assert.Throws<InvalidOperationException>(() => _ = failed.Value);
    }

    [Fact]
    public async Task ResultExtensions_EnsureTapAndMapAsync_ShouldComposeCleanly()
    {
        int sideEffect = 0;
        var res = await Task.FromResult(Result.Success(10)
            .Ensure(v => v > 5, new Error("Val.Small", "Too small"))
            .Tap(v => sideEffect = v))
            .MapAsync(v => v * 3);

        Assert.True(res.IsSuccess);
        Assert.Equal(10, sideEffect);
        Assert.Equal(30, res.Value);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1572864, "1.5 MB")]
    public void ByteSizeFormatter_FormatBytes_ShouldReturnExpectedString(long bytes, string expected)
    {
        Assert.Equal(expected, ByteSizeFormatter.FormatBytes(bytes));
    }

    [Fact]
    public void SecurityGuard_ValidateSafePath_ShouldBlockTraversalAndReservedNames()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmniRoot");
        var escaped = Path.Combine(root, "..", "Windows", "System32");

        var traversalResult = SecurityGuard.ValidateSafePath(escaped, root);
        Assert.True(traversalResult.IsFailure);
        Assert.Equal("Security.PathTraversal", traversalResult.Error.Code);

        var reservedResult = SecurityGuard.ValidateSafePath(Path.Combine(root, "CON.txt"));
        Assert.True(reservedResult.IsFailure);
        Assert.Equal("Security.ReservedDeviceName", reservedResult.Error.Code);
    }

    [Fact]
    public void BulkObservableCollection_ReplaceAll_ShouldFireSingleResetNotification()
    {
        var collection = new BulkObservableCollection<int>([1, 2, 3]);
        int notifications = 0;
        collection.CollectionChanged += (_, _) => notifications++;

        collection.ReplaceAll([10, 20, 30, 40, 50]);

        Assert.Equal(1, notifications);
        Assert.Equal(5, collection.Count);
    }

    private sealed class TestableToolViewModel : OmniForge.Core.Models.ToolViewModelBase
    {
        public Task<string?> ExecuteLongTaskAsync(CancellationToken ct = default)
        {
            return RunSafeAsync(
                async linkedToken =>
                {
                    ReportProgress(35, "Step 1...");
                    await Task.Delay(250, linkedToken);
                    ReportProgress(100, "Done");
                    return Result.Success("OK");
                },
                busyStatus: "Starting task...",
                successStatus: "Task finished.",
                cancellationToken: ct);
        }

        public void PromptConfirm(Func<CancellationToken, Task> action)
        {
            RequestConfirmation("Confirm Test", "Proceed with test?", action);
        }
    }

    [Fact]
    public async Task ToolViewModelBase_CancelCurrentOperation_ShouldAbortRunningTaskCleanly()
    {
        var vm = new TestableToolViewModel();
        var runningTask = vm.ExecuteLongTaskAsync();

        // Cancel while task is running
        vm.CancelCurrentOperation();

        var result = await runningTask;
        Assert.Null(result);
        Assert.False(vm.IsBusy);
        Assert.Contains("cancelled", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ToolViewModelBase_ConfirmationWorkflow_ShouldExecuteOrDismissCleanly()
    {
        var vm = new TestableToolViewModel();
        bool executed = false;

        vm.PromptConfirm(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        Assert.True(vm.IsConfirmationOpen);
        Assert.Equal("Confirm Test", vm.ConfirmationTitle);

        // Dismiss first
        vm.DismissConfirmation();
        Assert.False(vm.IsConfirmationOpen);
        Assert.False(executed);

        // Prompt again and confirm
        vm.PromptConfirm(async ct =>
        {
            var res = await vm.ExecuteLongTaskAsync(ct);
            executed = res == "OK";
        });

        await vm.ConfirmPendingActionAsync();
        Assert.False(vm.IsConfirmationOpen);
        Assert.True(executed);
        Assert.True(vm.HasSuccessMessage);
    }
}

