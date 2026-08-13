using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Members;
using BoardGameLibrary.Domain.Members;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLibrary.Infrastructure.Persistence.Repositories;

internal sealed class MemberRepository(BoardGameLibraryDbContext dbContext) : IMemberRepository
{
    private readonly BoardGameLibraryDbContext _dbContext = dbContext;

    public Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Members.SingleOrDefaultAsync(member => member.Id == id, cancellationToken);

    public async Task<Member?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RepositoryQuery.RequireExplicitTransaction(_dbContext);

        List<Member> rows = await _dbContext.Members
            .FromSqlInterpolated($"SELECT * FROM members WHERE id = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    public Task<MemberDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Members
            .AsNoTracking()
            .Where(member => member.Id == id)
            .Select(member => new MemberDetails(
                member.Id,
                member.MemberNumber,
                member.FullName,
                member.Email,
                member.PhoneNumber,
                member.IsActive,
                member.JoinedOn))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<MemberListItem>> ListAsync(
        ListMembersQuery query,
        CancellationToken cancellationToken)
    {
        bool isActive = query.IsActive
            ?? throw new InvalidOperationException(
                "The member active filter must be normalized by the application layer.");
        IQueryable<Member> filtered = _dbContext.Members
            .AsNoTracking()
            .Where(member => member.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = RepositoryQuery.LiteralContainsPattern(query.Search);
            filtered = filtered.Where(member =>
                EF.Functions.ILike(
                    member.FullName,
                    pattern,
                    RepositoryQuery.LikeEscapeCharacter)
                || EF.Functions.ILike(
                    member.Email,
                    pattern,
                    RepositoryQuery.LikeEscapeCharacter));
        }

        int totalCount = await filtered.CountAsync(cancellationToken);
        IQueryable<Member> ordered = ApplyOrdering(filtered, query.PageRequest);
        IQueryable<MemberListItem> projected = ordered.Select(member => new MemberListItem(
            member.Id,
            member.MemberNumber,
            member.FullName,
            member.Email,
            member.IsActive,
            member.JoinedOn));
        return await RepositoryQuery.ToPagedResultAsync(
            projected,
            query.PageRequest,
            totalCount,
            cancellationToken);
    }

    public Task<bool> ExistsWithNormalizedMemberNumberAsync(
        string normalizedMemberNumber,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        _dbContext.Members
            .AsNoTracking()
            .AnyAsync(
                member => member.MemberNumber == normalizedMemberNumber
                    && (!excludingId.HasValue || member.Id != excludingId.Value),
                cancellationToken);

    public Task<bool> ExistsWithNormalizedEmailAsync(
        string normalizedEmail,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        _dbContext.Members
            .AsNoTracking()
            .AnyAsync(
                member => member.NormalizedEmail == normalizedEmail
                    && (!excludingId.HasValue || member.Id != excludingId.Value),
                cancellationToken);

    public Task<bool> HasLoanHistoryAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Loans
            .AsNoTracking()
            .AnyAsync(loan => loan.MemberId == id, cancellationToken);

    public void Add(Member member) => _dbContext.Members.Add(member);

    public void Update(Member member) => _dbContext.Members.Update(member);

    public void Remove(Member member) => _dbContext.Members.Remove(member);

    private static IQueryable<Member> ApplyOrdering(
        IQueryable<Member> query,
        PageRequest pageRequest)
    {
        bool descending = pageRequest.SortDirection == SortDirection.Descending;

        IOrderedQueryable<Member> ordered = pageRequest.SortBy switch
        {
            MemberSortFields.MemberNumber => descending
                ? query.OrderByDescending(item => item.MemberNumber)
                : query.OrderBy(item => item.MemberNumber),
            MemberSortFields.JoinedOn => descending
                ? query.OrderByDescending(item => item.JoinedOn)
                : query.OrderBy(item => item.JoinedOn),
            _ => descending
                ? query.OrderByDescending(item => item.FullName)
                : query.OrderBy(item => item.FullName),
        };

        return descending
            ? ordered.ThenByDescending(item => item.Id)
            : ordered.ThenBy(item => item.Id);
    }
}
