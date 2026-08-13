namespace BoardGameLibrary.Application.Loans;

public sealed record CreateLoanCommand(
    Guid MemberId,
    Guid GameCopyId);

public sealed record ReturnLoanCommand(Guid Id);
