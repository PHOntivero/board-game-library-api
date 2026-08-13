using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.Members;

namespace BoardGameLibrary.Application.Members;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Acquires an update row lock and must be called inside an explicit transaction.
    /// In the loan-creation flow this lock follows the board-game shared lock and precedes the game-copy update lock.
    /// </summary>
    Task<Member?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<MemberDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<MemberListItem>> ListAsync(
        ListMembersQuery query,
        CancellationToken cancellationToken);

    Task<bool> ExistsWithNormalizedMemberNumberAsync(
        string normalizedMemberNumber,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<bool> ExistsWithNormalizedEmailAsync(
        string normalizedEmail,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<bool> HasLoanHistoryAsync(Guid id, CancellationToken cancellationToken);

    void Add(Member member);

    void Update(Member member);

    void Remove(Member member);
}
