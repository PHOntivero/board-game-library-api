using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.GameCopies;
using BoardGameLibrary.Domain.GameCopies;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLibrary.Infrastructure.Persistence.Repositories;

internal sealed class GameCopyRepository(BoardGameLibraryDbContext dbContext) : IGameCopyRepository
{
    private readonly BoardGameLibraryDbContext _dbContext = dbContext;

    public Task<GameCopy?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.GameCopies.SingleOrDefaultAsync(copy => copy.Id == id, cancellationToken);

    public async Task<GameCopy?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RepositoryQuery.RequireExplicitTransaction(_dbContext);

        List<GameCopy> rows = await _dbContext.GameCopies
            .FromSqlInterpolated($"SELECT * FROM game_copies WHERE id = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    public Task<GameCopyDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        (from copy in _dbContext.GameCopies.AsNoTracking()
         join boardGame in _dbContext.BoardGames.AsNoTracking()
             on copy.BoardGameId equals boardGame.Id
         where copy.Id == id
         select new GameCopyDetails(
             copy.Id,
             copy.BoardGameId,
             boardGame.Title,
             copy.InventoryCode,
             copy.Condition,
             copy.IsActive,
             copy.AcquiredOn,
             boardGame.IsActive
                 && copy.IsActive
                 && copy.Condition != GameCopyCondition.Damaged
                 && !_dbContext.Loans.Any(loan =>
                     loan.GameCopyId == copy.Id && loan.ReturnedAtUtc == null)))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<GameCopyListItem>> ListByBoardGameAsync(
        ListGameCopiesQuery query,
        CancellationToken cancellationToken)
    {
        bool isActive = query.IsActive
            ?? throw new InvalidOperationException(
                "The game-copy active filter must be normalized by the application layer.");
        IQueryable<GameCopy> filtered = _dbContext.GameCopies
            .AsNoTracking()
            .Where(copy =>
                copy.BoardGameId == query.BoardGameId
                && copy.IsActive == isActive);

        if (query.Condition.HasValue)
        {
            filtered = filtered.Where(copy => copy.Condition == query.Condition.Value);
        }

        if (query.IsAvailable.HasValue)
        {
            bool isAvailable = query.IsAvailable.Value;
            filtered = filtered.Where(copy =>
                (_dbContext.BoardGames.Any(boardGame =>
                    boardGame.Id == copy.BoardGameId && boardGame.IsActive)
                    && copy.IsActive
                    && copy.Condition != GameCopyCondition.Damaged
                    && !_dbContext.Loans.Any(loan =>
                        loan.GameCopyId == copy.Id && loan.ReturnedAtUtc == null)) == isAvailable);
        }

        int totalCount = await filtered.CountAsync(cancellationToken);
        IQueryable<GameCopy> ordered = ApplyOrdering(filtered, query.PageRequest);
        IQueryable<GameCopyListItem> projected = ordered.Select(copy => new GameCopyListItem(
            copy.Id,
            copy.BoardGameId,
            copy.InventoryCode,
            copy.Condition,
            copy.IsActive,
            copy.AcquiredOn,
            _dbContext.BoardGames.Any(boardGame =>
                boardGame.Id == copy.BoardGameId && boardGame.IsActive)
                && copy.IsActive
                && copy.Condition != GameCopyCondition.Damaged
                && !_dbContext.Loans.Any(loan =>
                    loan.GameCopyId == copy.Id && loan.ReturnedAtUtc == null)));
        return await RepositoryQuery.ToPagedResultAsync(
            projected,
            query.PageRequest,
            totalCount,
            cancellationToken);
    }

    public Task<bool> ExistsWithNormalizedInventoryCodeAsync(
        string normalizedInventoryCode,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        _dbContext.GameCopies
            .AsNoTracking()
            .AnyAsync(
                copy => copy.InventoryCode == normalizedInventoryCode
                    && (!excludingId.HasValue || copy.Id != excludingId.Value),
                cancellationToken);

    public Task<bool> HasLoanHistoryAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Loans
            .AsNoTracking()
            .AnyAsync(loan => loan.GameCopyId == id, cancellationToken);

    public void Add(GameCopy gameCopy) => _dbContext.GameCopies.Add(gameCopy);

    public void Update(GameCopy gameCopy) => _dbContext.GameCopies.Update(gameCopy);

    public void Remove(GameCopy gameCopy) => _dbContext.GameCopies.Remove(gameCopy);

    private static IQueryable<GameCopy> ApplyOrdering(
        IQueryable<GameCopy> query,
        PageRequest pageRequest)
    {
        bool descending = pageRequest.SortDirection == SortDirection.Descending;

        IOrderedQueryable<GameCopy> ordered = pageRequest.SortBy switch
        {
            GameCopySortFields.Condition => descending
                ? query.OrderByDescending(item => item.Condition)
                : query.OrderBy(item => item.Condition),
            GameCopySortFields.AcquiredOn => descending
                ? query.OrderByDescending(item => item.AcquiredOn)
                : query.OrderBy(item => item.AcquiredOn),
            _ => descending
                ? query.OrderByDescending(item => item.InventoryCode)
                : query.OrderBy(item => item.InventoryCode),
        };

        return descending
            ? ordered.ThenByDescending(item => item.Id)
            : ordered.ThenBy(item => item.Id);
    }
}
