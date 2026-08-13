using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.UnitTests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_HasNoErrors()
    {
        Result result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_ExposesItsErrors()
    {
        Error first = Error.Validation("first", "First error.");
        Error second = Error.Validation("second", "Second error.");

        Result result = Result.Failure([first, second]);

        Assert.True(result.IsFailure);
        Assert.Equal([first, second], result.Errors);
    }

    [Fact]
    public void GenericSuccess_ExposesItsValue()
    {
        Result<string> result = Result<string>.Success("created-id");

        Assert.True(result.IsSuccess);
        Assert.Equal("created-id", result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void GenericFailure_ValueAccessThrowsInvalidOperationException()
    {
        Result<string> result = Result<string>.Failure(
            Error.NotFound("missing", "The value was not found."));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void GenericSuccess_WithNullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }

    [Fact]
    public void Failure_WithNoErrors_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure([]));
        Assert.Throws<ArgumentException>(() => Result<int>.Failure([]));
    }

    [Fact]
    public void Failure_WithMixedErrorTypes_ThrowsArgumentException()
    {
        Error validation = Error.Validation("validation", "Validation failed.");
        Error conflict = Error.Conflict("conflict", "A conflict occurred.");

        Assert.Throws<ArgumentException>(() => Result.Failure([validation, conflict]));
        Assert.Throws<ArgumentException>(() => Result<int>.Failure([validation, conflict]));
    }
}
