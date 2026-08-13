using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Loans;
using BoardGameLibrary.Domain.Loans;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLibrary.Infrastructure.Persistence.Repositories;

internal sealed class LoanRepository(BoardGameLibraryDbContext dbContext) : ILoanRepository
{
    private readonly BoardGameLibraryDbContext _dbContext = dbContext;

    public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Loans.SingleOrDefaultAsync(loan => loan.Id == id, cancellationToken);

    public async Task<Loan?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RepositoryQuery.RequireExplicitTransaction(_dbContext);

        List<Loan> rows = await _dbContext.Loans
            .FromSqlInterpolated($"SELECT * FROM loans WHERE id = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    public Task<LoanDetails?> GetDetailsAsync(
        Guid id,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) =>
        (from loan in _dbContext.Loans.AsNoTracking()
         join copy in _dbContext.GameCopies.AsNoTracking()
             on loan.GameCopyId equals copy.Id
         join boardGame in _dbContext.BoardGames.AsNoTracking()
             on copy.BoardGameId equals boardGame.Id
         join member in _dbContext.Members.AsNoTracking()
             on loan.MemberId equals member.Id
         where loan.Id == id
         select new LoanDetails(
             loan.Id,
             new LoanGameCopy(copy.Id, copy.InventoryCode, boardGame.Id, boardGame.Title),
             new LoanMember(member.Id, member.MemberNumber, member.FullName),
             loan.LoanedAtUtc,
             loan.DueAtUtc,
             loan.ReturnedAtUtc,
             loan.ReturnedAtUtc != null
                 ? LoanStatus.Returned
                 : utcNow > loan.DueAtUtc
                     ? LoanStatus.Overdue
                     : LoanStatus.Active))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<LoanListItem>> ListAsync(
        ListLoansQuery query,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        IQueryable<Loan> filtered = _dbContext.Loans.AsNoTracking();

        if (query.MemberId.HasValue)
        {
            filtered = filtered.Where(loan => loan.MemberId == query.MemberId.Value);
        }

        if (query.GameCopyId.HasValue)
        {
            filtered = filtered.Where(loan => loan.GameCopyId == query.GameCopyId.Value);
        }

        if (query.Status.HasValue)
        {
            filtered = query.Status.Value switch
            {
                LoanStatus.Returned => filtered.Where(loan => loan.ReturnedAtUtc != null),
                LoanStatus.Overdue => filtered.Where(loan =>
                    loan.ReturnedAtUtc == null && utcNow > loan.DueAtUtc),
                _ => filtered.Where(loan =>
                    loan.ReturnedAtUtc == null && utcNow <= loan.DueAtUtc),
            };
        }

        if (query.LoanedFromUtc.HasValue)
        {
            filtered = filtered.Where(loan => loan.LoanedAtUtc >= query.LoanedFromUtc.Value);
        }

        if (query.LoanedToUtc.HasValue)
        {
            filtered = filtered.Where(loan => loan.LoanedAtUtc <= query.LoanedToUtc.Value);
        }

        int totalCount = await filtered.CountAsync(cancellationToken);

        IQueryable<Loan> ordered = ApplyOrdering(filtered, query.PageRequest);
        IQueryable<LoanListItem> projected =
            from loan in ordered
            join copy in _dbContext.GameCopies.AsNoTracking()
                on loan.GameCopyId equals copy.Id
            join boardGame in _dbContext.BoardGames.AsNoTracking()
                on copy.BoardGameId equals boardGame.Id
            join member in _dbContext.Members.AsNoTracking()
                on loan.MemberId equals member.Id
            select new LoanListItem(
                loan.Id,
                new LoanGameCopy(copy.Id, copy.InventoryCode, boardGame.Id, boardGame.Title),
                new LoanMember(member.Id, member.MemberNumber, member.FullName),
                loan.LoanedAtUtc,
                loan.DueAtUtc,
                loan.ReturnedAtUtc,
                loan.ReturnedAtUtc != null
                    ? LoanStatus.Returned
                    : utcNow > loan.DueAtUtc
                        ? LoanStatus.Overdue
                        : LoanStatus.Active);

        return await RepositoryQuery.ToPagedResultAsync(
            projected,
            query.PageRequest,
            totalCount,
            cancellationToken);
    }

    public Task<bool> HasOpenLoanForGameCopyAsync(
        Guid gameCopyId,
        CancellationToken cancellationToken) =>
        _dbContext.Loans
            .AsNoTracking()
            .AnyAsync(
                loan => loan.GameCopyId == gameCopyId && loan.ReturnedAtUtc == null,
                cancellationToken);

    public Task<int> CountOpenLoansForMemberAsync(
        Guid memberId,
        CancellationToken cancellationToken) =>
        _dbContext.Loans
            .AsNoTracking()
            .CountAsync(
                loan => loan.MemberId == memberId && loan.ReturnedAtUtc == null,
                cancellationToken);

    public Task<bool> HasOverdueLoanForMemberAsync(
        Guid memberId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) =>
        _dbContext.Loans
            .AsNoTracking()
            .AnyAsync(
                loan => loan.MemberId == memberId
                    && loan.ReturnedAtUtc == null
                    && utcNow > loan.DueAtUtc,
                cancellationToken);

    public void Add(Loan loan) => _dbContext.Loans.Add(loan);

    public void Update(Loan loan) => _dbContext.Loans.Update(loan);

    private static IQueryable<Loan> ApplyOrdering(
        IQueryable<Loan> query,
        PageRequest pageRequest)
    {
        bool descending = pageRequest.SortDirection == SortDirection.Descending;

        IOrderedQueryable<Loan> ordered = pageRequest.SortBy switch
        {
            LoanSortFields.DueAtUtc => descending
                ? query.OrderByDescending(item => item.DueAtUtc)
                : query.OrderBy(item => item.DueAtUtc),
            LoanSortFields.ReturnedAtUtc => descending
                ? query.OrderByDescending(item => item.ReturnedAtUtc)
                : query.OrderBy(item => item.ReturnedAtUtc),
            _ => descending
                ? query.OrderByDescending(item => item.LoanedAtUtc)
                : query.OrderBy(item => item.LoanedAtUtc),
        };

        return descending
            ? ordered.ThenByDescending(item => item.Id)
            : ordered.ThenBy(item => item.Id);
    }
}
