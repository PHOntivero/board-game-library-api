using BoardGameLibrary.Domain.Loans;

namespace BoardGameLibrary.Application.Loans;

public sealed record LoanGameCopy(
    Guid Id,
    string InventoryCode,
    Guid BoardGameId,
    string BoardGameTitle);

public sealed record LoanMember(
    Guid Id,
    string MemberNumber,
    string FullName);

public sealed record LoanListItem(
    Guid Id,
    LoanGameCopy GameCopy,
    LoanMember Member,
    DateTimeOffset LoanedAtUtc,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    LoanStatus Status);

public sealed record LoanDetails(
    Guid Id,
    LoanGameCopy GameCopy,
    LoanMember Member,
    DateTimeOffset LoanedAtUtc,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    LoanStatus Status);
