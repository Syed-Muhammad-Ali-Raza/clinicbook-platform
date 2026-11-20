using ClinicBook.Domain.Common;
using FluentAssertions;
using Xunit;

namespace ClinicBook.Domain.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        var error = Error.Validation("Test.Code", "Something went wrong");
        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void GenericSuccess_ShouldExposeValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_ShouldThrowWhenAccessingValue()
    {
        var error = Error.NotFound("Test.NotFound", "Missing");
        var result = Result.Failure<int>(error);

        result.IsFailure.Should().BeTrue();
        var act = () => _ = result.Value;
        act.Should().Throw<InvalidOperationException>();
    }
}
