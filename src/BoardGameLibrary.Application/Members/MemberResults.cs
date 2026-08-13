namespace BoardGameLibrary.Application.Members;

public sealed record MemberListItem(
    Guid Id,
    string MemberNumber,
    string FullName,
    string Email,
    bool IsActive,
    DateOnly JoinedOn);

public sealed record MemberDetails(
    Guid Id,
    string MemberNumber,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    DateOnly JoinedOn);
