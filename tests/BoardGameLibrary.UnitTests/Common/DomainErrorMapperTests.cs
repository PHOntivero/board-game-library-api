using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.UnitTests.Common;

public sealed class DomainErrorMapperTests
{
    [Fact]
    public void Map_ValidationError_UsesStableValidationCode()
    {
        var exception = new DomainException(
            "board_game.title.required",
            "Board game title is required.",
            DomainErrorType.Validation);

        Error error = DomainErrorMapper.Map(exception);

        Assert.Equal(ErrorCodes.Common.ValidationFailed, error.Code);
        Assert.Equal("Board game title is required.", error.Description);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.DoesNotContain(exception.Code, error.Code, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ErrorCodes.Categories.Inactive)]
    [InlineData(ErrorCodes.GameCopies.HasOpenLoan)]
    [InlineData(ErrorCodes.Members.LoanLimitReached)]
    [InlineData(ErrorCodes.Loans.AlreadyReturned)]
    public void Map_AllowlistedConflict_PreservesPublicCode(string publicCode)
    {
        var exception = new DomainException(
            publicCode,
            "The operation conflicts with the current state.",
            DomainErrorType.Conflict);

        Error error = DomainErrorMapper.Map(exception);

        Assert.Equal(publicCode, error.Code);
        Assert.Equal(exception.Message, error.Description);
        Assert.Equal(ErrorType.Conflict, error.Type);
    }

    [Theory]
    [InlineData("loan.internal_conflict")]
    [InlineData("board_game.category_inactive")]
    [InlineData("not_a_public_code")]
    public void Map_NonAllowlistedConflict_UsesSafeFallback(string internalCode)
    {
        var exception = new DomainException(
            internalCode,
            "Sensitive internal conflict detail.",
            DomainErrorType.Conflict);

        Error error = DomainErrorMapper.Map(exception);

        Assert.Equal(ErrorCodes.Common.BusinessRuleConflict, error.Code);
        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.DoesNotContain(internalCode, error.Code, StringComparison.Ordinal);
        Assert.DoesNotContain("Sensitive", error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_WithNullException_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DomainErrorMapper.Map(null!));
    }
}
