using BoardGameLibrary.Api.Contracts;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.GameCopies;

namespace BoardGameLibrary.Api.Contracts.GameCopies;

public sealed record GameCopyListItemResponse(
    Guid Id,
    Guid BoardGameId,
    string InventoryCode,
    GameCopyCondition Condition,
    bool IsActive,
    DateOnly? AcquiredOn,
    bool IsAvailable);

public sealed record GameCopyResponse(
    Guid Id,
    Guid BoardGameId,
    string BoardGameTitle,
    string InventoryCode,
    GameCopyCondition Condition,
    bool IsActive,
    DateOnly? AcquiredOn,
    bool IsAvailable);

internal static class GameCopyResponseMappings
{
    internal static GameCopyResponse ToResponse(
        this Application.GameCopies.GameCopyDetails source) =>
        new(
            source.Id,
            source.BoardGameId,
            source.BoardGameTitle,
            source.InventoryCode,
            source.Condition,
            source.IsActive,
            source.AcquiredOn,
            source.IsAvailable);

    internal static PagedResponse<GameCopyListItemResponse> ToResponse(
        this PagedResult<Application.GameCopies.GameCopyListItem> source) =>
        new(
            source.Items
                .Select(item => new GameCopyListItemResponse(
                    item.Id,
                    item.BoardGameId,
                    item.InventoryCode,
                    item.Condition,
                    item.IsActive,
                    item.AcquiredOn,
                    item.IsAvailable))
                .ToArray(),
            source.Page,
            source.PageSize,
            source.TotalCount,
            source.TotalPages);
}
