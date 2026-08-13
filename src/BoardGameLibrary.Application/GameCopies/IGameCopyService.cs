using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Application.GameCopies;

public interface IGameCopyService
{
    Task<Result<Guid>> CreateAsync(
        CreateGameCopyCommand command,
        CancellationToken cancellationToken);

    Task<Result<GameCopyDetails>> GetAsync(
        GetGameCopyQuery query,
        CancellationToken cancellationToken);

    Task<Result<PagedResult<GameCopyListItem>>> ListAsync(
        ListGameCopiesQuery query,
        CancellationToken cancellationToken);

    Task<Result<GameCopyDetails>> UpdateAsync(
        UpdateGameCopyCommand command,
        CancellationToken cancellationToken);

    Task<Result> DeleteAsync(
        DeleteGameCopyCommand command,
        CancellationToken cancellationToken);
}
