using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.Domain.Loans;

public sealed class Loan
{
    public const int LendingTermDays = 14;

    private Loan()
    {
    }

    public Guid Id { get; private set; }

    public Guid GameCopyId { get; private set; }

    public Guid MemberId { get; private set; }

    public DateTimeOffset LoanedAtUtc { get; private set; }

    public DateTimeOffset DueAtUtc { get; private set; }

    public DateTimeOffset? ReturnedAtUtc { get; private set; }

    public static Loan Create(Guid gameCopyId, Guid memberId, DateTimeOffset loanedAtUtc)
    {
        DomainGuard.NotEmpty(gameCopyId, "Game copy identifier", "loan.game_copy_id_required");
        DomainGuard.NotEmpty(memberId, "Member identifier", "loan.member_id_required");
        DomainGuard.Utc(loanedAtUtc, "Loan date", "loan.loaned_at_not_utc");

        return new Loan
        {
            Id = Guid.CreateVersion7(),
            GameCopyId = gameCopyId,
            MemberId = memberId,
            LoanedAtUtc = loanedAtUtc,
            DueAtUtc = loanedAtUtc.AddDays(LendingTermDays),
        };
    }

    public LoanStatus GetStatus(DateTimeOffset utcNow)
    {
        DomainGuard.Utc(utcNow, "Current date", "loan.current_time_not_utc");

        if (ReturnedAtUtc.HasValue)
        {
            return LoanStatus.Returned;
        }

        return utcNow > DueAtUtc ? LoanStatus.Overdue : LoanStatus.Active;
    }

    public void Return(DateTimeOffset returnedAtUtc)
    {
        DomainGuard.Utc(returnedAtUtc, "Return date", "loan.returned_at_not_utc");

        if (ReturnedAtUtc.HasValue)
        {
            throw new DomainException(
                "loan_already_returned",
                "A returned loan cannot be returned again.",
                DomainErrorType.Conflict);
        }

        if (returnedAtUtc < LoanedAtUtc)
        {
            throw new DomainException(
                "loan.returned_before_loan",
                "Return date cannot be earlier than the loan date.",
                DomainErrorType.Validation);
        }

        ReturnedAtUtc = returnedAtUtc;
    }
}
