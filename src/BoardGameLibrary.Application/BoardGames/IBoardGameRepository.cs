using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.BoardGames;

namespace BoardGameLibrary.Application.BoardGames;

public interface IBoardGameRepository
{
    /// <summary>
    /// Gets the tracked aggregate for mutation with its <see cref="BoardGame.Categories" /> collection loaded.
    /// This method does not acquire a database row lock.
    /// </summary>
    Task<BoardGame?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Acquires a shared row lock. It must be called inside an explicit transaction.
    /// In the loan-creation flow this is the first lock, before the member and game-copy update locks.
    /// </summary>
    Task<BoardGame?> GetByIdForShareAsync(Guid id, CancellationToken cancellationToken);

    Task<BoardGameDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<BoardGameListItem>> ListAsync(
        ListBoardGamesQuery query,
        CancellationToken cancellationToken);

    Task<bool> HasCopiesAsync(Guid id, CancellationToken cancellationToken);

    void Add(BoardGame boardGame);

    void Update(BoardGame boardGame);

    void Remove(BoardGame boardGame);
}
