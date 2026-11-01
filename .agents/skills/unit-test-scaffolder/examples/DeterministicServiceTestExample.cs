using OmniForge.Core.Common;

namespace OmniForge.Tests.Services;

public sealed class DeterministicServiceTestExample
{
    [Fact]
    public async Task ProcessAsync_WithValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var fakeService = new FakeTextProcessor();

        // Act
        var result = await fakeService.ProcessAsync("hello-omni");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("PROCESSED:hello-omni", result.Value);
    }

    [Fact]
    public async Task ProcessAsync_WithEmptyInput_ReturnsFailureResult()
    {
        // Arrange
        var fakeService = new FakeTextProcessor();

        // Act
        var result = await fakeService.ProcessAsync(string.Empty);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Processor.InvalidInput", result.Error.Code);
    }

    private sealed class FakeTextProcessor
    {
        public Task<Result<string>> ProcessAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Task.FromResult(Result.Failure<string>("Processor.InvalidInput", "Input text cannot be blank."));

            return Task.FromResult(Result.Success($"PROCESSED:{input}"));
        }
    }
}

