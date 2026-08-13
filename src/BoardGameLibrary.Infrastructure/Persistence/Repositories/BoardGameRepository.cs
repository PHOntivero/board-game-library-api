using BoardGameLibrary.Application.BoardGames;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.GameCopies;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLibrary.Infrastructure.Persistence.Repositories;

internal sealed class BoardGameRepository(BoardGameLibraryDbContext dbContext) : IBoardGameRepository
{
    private readonly BoardGameLibraryDbContext _dbContext = dbContext;

    public Task<BoardGame?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.BoardGames
            .Include(boardGame => boardGame.Categories)
            .SingleOrDefaultAsync(boardGame => boardGame.Id == id, cancellationToken);

    public async Task<BoardGame?> GetByIdForShareAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RepositoryQuery.RequireExplicitTransaction(_dbContext);

        List<BoardGame> rows = await _dbContext.BoardGames
            .FromSqlInterpolated($"SELECT * FROM board_games WHERE id = {id} FOR SHARE")
            .ToListAsync(cancellationToken);

        return rows.SingleOrDefault();
    }

    public Task<BoardGameDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.BoardGames
            .AsNoTracking()
            .Where(boardGame => boardGame.Id == id)
            .Select(boardGame => new BoardGameDetails(
                boardGame.Id,
                boardGame.Title,
                boardGame.Publisher,
                boardGame.Description,
                boardGame.PublicationYear,
                boardGame.MinPlayers,
                boardGame.MaxPlayers,
                boardGame.PlayingTimeMinutes,
                boardGame.IsActive,
                _dbContext.GameCopies.Count(copy => copy.BoardGameId == boardGame.Id),
                _dbContext.GameCopies.Count(copy =>
                    copy.BoardGameId == boardGame.Id
                    && boardGame.IsActive
                    && copy.IsActive
                    && copy.Condition != GameCopyCondition.Damaged
                    && !_dbContext.Loans.Any(loan =>
                        loan.GameCopyId == copy.Id && loan.ReturnedAtUtc == null)),
                boardGame.IsActive && _dbContext.GameCopies.Any(copy =>
                    copy.BoardGameId == boardGame.Id
                    && copy.IsActive
                    && copy.Condition != GameCopyCondition.Damaged
                    && !_dbContext.Loans.Any(loan =>
                        loan.GameCopyId == copy.Id && loan.ReturnedAtUtc == null)),
                boardGame.Categories
                    .OrderBy(category => category.Name)
                    .ThenBy(category => category.Id)
                    .Select(category => new BoardGameLibrary.Application.BoardGames.BoardGameCategory(
                        category.Id,
                        category.Name,
                        category.IsActive))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<BoardGameListItem>> ListAsync(
        ListBoardGamesQuery query,
        CancellationToken cancellationToken)
    {
        bool isActive = query.IsActive
            ?? throw new InvalidOperationException(
                "The board-game active filter must be normalized by the application layer.");
        IQueryable<BoardGame> filtered = _dbContext.BoardGames.AsNoTracking();

        filtered = filtered.Where(boardGame => boardGame.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = RepositoryQuery.LiteralContainsPattern(query.Search);
            filtered = filtered.Where(boardGame => EF.Functions.ILike(
                boardGame.Title,
                pattern,
                RepositoryQuery.LikeEscapeCharacter));
        }

        if (query.CategoryId.HasValue)
        {
            Guid categoryId = query.CategoryId.Value;
            filtered = filtered.Where(boardGame =>
                boardGame.Categories.Any(category => category.Id == categoryId));
        }

        if (query.Players.HasValue)
        {
            int players = query.Players.Value;
            filtered = filtered.Where(boardGame =>
                boardGame.MinPlayers <= players && boardGame.MaxPlayers >= players);
        }

        if (query.IsAvailable.HasValue)
        {
            bool isAvailable = query.IsAvailable.Value;
            filtered = filtered.Where(boardGame =>
                (_dbContext.GameCopies.Any(copy =>
                    copy.BoardGameId == boardGame.Id
                    && boardGame.IsActive
                    && copy.IsActive
                    && copy.Condition != GameCopyCondition.Damaged
                    && !_dbContext.Loans.Any(loan =>
                        loan.GameCopyId == copy.Id && loan.ReturnedAtUtc == null))) == isAvailable);
        }

        int totalCount = await filtered.CountAsync(cancellationToken);

        IQueryable<BoardGame> ordered = ApplyOrdering(filtered, query.PageRequest);
        IQueryable<BoardGameListItem> projected = ordered.Select(boardGame => new BoardGameListItem(
            boardGame.Id,
            boardGame.Title,
            boardGame.Publisher,
            boardGame.PublicationYear,
            boardGame.MinPlayers,
            boardGame.MaxPlayers,
            boardGame.PlayingTimeMinutes,
            boardGame.IsActive,
            boardGame.IsActive && _dbContext.GameCopies.Any(copy =>
                copy.BoardGameId == boardGame.Id
                && copy.IsActive
                && copy.Condition != GameCopyCondition.Damaged
                && !_dbContext.Loans.Any(loan =>
                    loan.GameCopyId == copy.Id && loan.ReturnedAtUtc == null))));

        return await RepositoryQuery.ToPagedResultAsync(
            projected,
            query.PageRequest,
            totalCount,
            cancellationToken);
    }

    public Task<bool> HasCopiesAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.GameCopies
            .AsNoTracking()
            .AnyAsync(copy => copy.BoardGameId == id, cancellationToken);

    public void Add(BoardGame boardGame) => _dbContext.BoardGames.Add(boardGame);

    public void Update(BoardGame boardGame) =>
        _dbContext.Entry(boardGame).State = EntityState.Modified;

    public void Remove(BoardGame boardGame) => _dbContext.BoardGames.Remove(boardGame);

    private static IQueryable<BoardGame> ApplyOrdering(
        IQueryable<BoardGame> query,
        PageRequest pageRequest)
    {
        bool descending = pageRequest.SortDirection == SortDirection.Descending;

        IOrderedQueryable<BoardGame> ordered = pageRequest.SortBy switch
        {
            BoardGameSortFields.Publisher => descending
                ? query.OrderByDescending(item => item.Publisher)
                : query.OrderBy(item => item.Publisher),
            BoardGameSortFields.PublicationYear => descending
                ? query.OrderByDescending(item => item.PublicationYear)
                : query.OrderBy(item => item.PublicationYear),
            BoardGameSortFields.MinPlayers => descending
                ? query.OrderByDescending(item => item.MinPlayers)
                : query.OrderBy(item => item.MinPlayers),
            BoardGameSortFields.MaxPlayers => descending
                ? query.OrderByDescending(item => item.MaxPlayers)
                : query.OrderBy(item => item.MaxPlayers),
            BoardGameSortFields.PlayingTimeMinutes => descending
                ? query.OrderByDescending(item => item.PlayingTimeMinutes)
                : query.OrderBy(item => item.PlayingTimeMinutes),
            _ => descending
                ? query.OrderByDescending(item => item.Title)
                : query.OrderBy(item => item.Title),
        };

        return descending
            ? ordered.ThenByDescending(item => item.Id)
            : ordered.ThenBy(item => item.Id);
    }
}
