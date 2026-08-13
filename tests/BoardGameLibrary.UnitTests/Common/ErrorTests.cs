using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.UnitTests.Common;

public sealed class ErrorTests
{
    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    public void Constructor_WithValidValues_CreatesError(ErrorType type)
    {
        var error = new Error("stable_code", "A useful description.", type);

        Assert.Equal("stable_code", error.Code);
        Assert.Equal("A useful description.", error.Description);
        Assert.Equal(type, error.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCode_ThrowsArgumentException(string? code)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new Error(code!, "A useful description.", ErrorType.Validation));
    }

    [Fact]
    public void Constructor_WithUndefinedType_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Error("stable_code", "A useful description.", (ErrorType)99));
    }

    [Fact]
    public void FactoryMethods_AssignExpectedTypes()
    {
        Error validation = Error.Validation("validation", "Validation failed.");
        Error notFound = Error.NotFound("not_found", "The resource was not found.");
        Error conflict = Error.Conflict("conflict", "The state conflicts with the operation.");

        Assert.Equal(ErrorType.Validation, validation.Type);
        Assert.Equal(ErrorType.NotFound, notFound.Type);
        Assert.Equal(ErrorType.Conflict, conflict.Type);
    }
}
