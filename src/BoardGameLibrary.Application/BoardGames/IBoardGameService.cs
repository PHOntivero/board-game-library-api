using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Application.BoardGames;

public interface IBoardGameService
{
    Task<Result<Guid>> CreateAsync(
        CreateBoardGameCommand command,
        CancellationToken cancellationToken);

    Task<Result<BoardGameDetails>> GetAsync(
        GetBoardGameQuery query,
        CancellationToken cancellationToken);

    Task<Result<PagedResult<BoardGameListItem>>> ListAsync(
        ListBoardGamesQuery query,
        CancellationToken cancellationToken);

    Task<Result<BoardGameDetails>> UpdateAsync(
        UpdateBoardGameCommand command,
        CancellationToken cancellationToken);

    Task<Result> DeleteAsync(
        DeleteBoardGameCommand command,
        CancellationToken cancellationToken);
}
