using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.Loans;

namespace BoardGameLibrary.Application.Loans;

public sealed record GetLoanQuery(Guid Id);

public sealed record ListLoansQuery(
    Guid? MemberId,
    Guid? GameCopyId,
    LoanStatus? Status,
    DateTimeOffset? LoanedFromUtc,
    DateTimeOffset? LoanedToUtc,
    PageRequest PageRequest);

public static class LoanSortFields
{
    public const string LoanedAtUtc = "loanedAtUtc";
    public const string DueAtUtc = "dueAtUtc";
    public const string ReturnedAtUtc = "returnedAtUtc";

    public const string Default = LoanedAtUtc;

    public static IReadOnlyCollection<string> Allowed { get; } =
        Array.AsReadOnly([LoanedAtUtc, DueAtUtc, ReturnedAtUtc]);

    public const SortDirection DefaultDirection = SortDirection.Descending;
}
