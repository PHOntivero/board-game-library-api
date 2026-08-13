using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Application.Members;

public interface IMemberService
{
    Task<Result<Guid>> CreateAsync(
        CreateMemberCommand command,
        CancellationToken cancellationToken);

    Task<Result<MemberDetails>> GetAsync(
        GetMemberQuery query,
        CancellationToken cancellationToken);

    Task<Result<PagedResult<MemberListItem>>> ListAsync(
        ListMembersQuery query,
        CancellationToken cancellationToken);

    Task<Result<MemberDetails>> UpdateAsync(
        UpdateMemberCommand command,
        CancellationToken cancellationToken);

    Task<Result> DeleteAsync(
        DeleteMemberCommand command,
        CancellationToken cancellationToken);
}
