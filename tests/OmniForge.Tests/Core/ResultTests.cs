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
}

