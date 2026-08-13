using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.Application.Common;

public static class DomainErrorMapper
{
    private const string GenericConflictDescription =
        "The requested operation conflicts with a business rule.";

    private static readonly HashSet<string> AllowedConflictCodes = new(StringComparer.Ordinal)
    {
        ErrorCodes.BoardGames.Inactive,
        ErrorCodes.BoardGames.HasCopies,
        ErrorCodes.Categories.DuplicateName,
        ErrorCodes.Categories.Inactive,
        ErrorCodes.Categories.HasBoardGames,
        ErrorCodes.GameCopies.DuplicateInventoryCode,
        ErrorCodes.GameCopies.Inactive,
        ErrorCodes.GameCopies.Damaged,
        ErrorCodes.GameCopies.Unavailable,
        ErrorCodes.GameCopies.HasOpenLoan,
        ErrorCodes.GameCopies.HasLoanHistory,
        ErrorCodes.Members.DuplicateMemberNumber,
        ErrorCodes.Members.DuplicateEmail,
        ErrorCodes.Members.Inactive,
        ErrorCodes.Members.LoanLimitReached,
        ErrorCodes.Members.HasOverdueLoan,
        ErrorCodes.Members.HasLoanHistory,
        ErrorCodes.Loans.AlreadyReturned,
    };

    public static Error Map(DomainException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Type switch
        {
            DomainErrorType.Validation => Error.Validation(
                ErrorCodes.Common.ValidationFailed,
                exception.Message),
            DomainErrorType.Conflict => MapConflict(exception),
            _ => throw new InvalidOperationException("The domain error type is not supported."),
        };
    }

    private static Error MapConflict(DomainException exception)
    {
        if (AllowedConflictCodes.Contains(exception.Code))
        {
            return Error.Conflict(exception.Code, exception.Message);
        }

        return Error.Conflict(
            ErrorCodes.Common.BusinessRuleConflict,
            GenericConflictDescription);
    }
}
