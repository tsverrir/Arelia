using Arelia.Domain.Common;
using FluentAssertions;

namespace Arelia.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void SuccessResultShouldHaveIsSuccessTrue()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void FailureResultShouldHaveErrorMessage()
    {
        var result = Result.Failure("Something went wrong");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Something went wrong");
    }

    [Fact]
    public void GenericSuccessResultShouldContainValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailureResultShouldHaveDefaultValue()
    {
        var result = Result.Failure<int>("Error");

        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(default);
        result.Error.Should().Be("Error");
    }

    [Fact]
    public void SuccessWithErrorShouldThrow()
    {
        var act = () => new TestResult(true, "error");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FailureWithoutErrorShouldThrow()
    {
        var act = () => new TestResult(false, null);

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Concrete subclass to test the protected constructor.
    /// </summary>
    private class TestResult(bool isSuccess, string? error) : Result(isSuccess, error);
}
