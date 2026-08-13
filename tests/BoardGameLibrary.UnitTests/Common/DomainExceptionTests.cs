using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.UnitTests.Common;

public sealed class DomainExceptionTests
{
    [Theory]
    [InlineData(DomainErrorType.Validation)]
    [InlineData(DomainErrorType.Conflict)]
    public void Constructor_WithValidValues_ExposesDomainError(DomainErrorType type)
    {
        var exception = new DomainException("stable_code", "A useful message.", type);

        Assert.Equal("stable_code", exception.Code);
        Assert.Equal("A useful message.", exception.Message);
        Assert.Equal(type, exception.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenCodeIsInvalid_ThrowsArgumentException(string? code)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new DomainException(code!, "A useful message.", DomainErrorType.Validation));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenMessageIsInvalid_ThrowsArgumentException(string? message)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new DomainException("stable_code", message!, DomainErrorType.Validation));
    }

    [Fact]
    public void Constructor_WhenTypeIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DomainException("stable_code", "A useful message.", (DomainErrorType)99));
    }
}
