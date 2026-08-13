using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Application.Members;

public sealed record GetMemberQuery(Guid Id);

public sealed record ListMembersQuery(
    string? Search,
    bool? IsActive,
    PageRequest PageRequest);

public static class MemberSortFields
{
    public const string FullName = "fullName";
    public const string MemberNumber = "memberNumber";
    public const string JoinedOn = "joinedOn";

    public const string Default = FullName;

    public static IReadOnlyCollection<string> Allowed { get; } =
        Array.AsReadOnly([FullName, MemberNumber, JoinedOn]);

    public const SortDirection DefaultDirection = SortDirection.Ascending;
}
