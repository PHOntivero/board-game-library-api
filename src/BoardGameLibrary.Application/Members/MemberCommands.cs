namespace BoardGameLibrary.Application.Members;

public sealed record CreateMemberCommand(
    string MemberNumber,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateOnly JoinedOn);

public sealed record UpdateMemberCommand(
    Guid Id,
    string MemberNumber,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateOnly JoinedOn,
    bool IsActive);

public sealed record DeleteMemberCommand(Guid Id);
