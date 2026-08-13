using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.GameCopies;

namespace BoardGameLibrary.Application.GameCopies;

public interface IGameCopyRepository
{
    Task<GameCopy?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Acquires an update row lock and must be called inside an explicit transaction.
    /// In the loan-creation flow this is the final lock, after the board-game shared lock and member update lock.
    /// </summary>
    Task<GameCopy?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<GameCopyDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<GameCopyListItem>> ListByBoardGameAsync(
        ListGameCopiesQuery query,
        CancellationToken cancellationToken);

    Task<bool> ExistsWithNormalizedInventoryCodeAsync(
        string normalizedInventoryCode,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<bool> HasLoanHistoryAsync(Guid id, CancellationToken cancellationToken);

    void Add(GameCopy gameCopy);

    void Update(GameCopy gameCopy);

    void Remove(GameCopy gameCopy);
}
