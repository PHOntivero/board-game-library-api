using BoardGameLibrary.Api.Contracts;
using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Api.Contracts.BoardGames;

public sealed record BoardGameCategoryResponse(
    Guid Id,
    string Name,
    bool IsActive);

public sealed record BoardGameListItemResponse(
    Guid Id,
    string Title,
    string Publisher,
    int PublicationYear,
    int MinPlayers,
    int MaxPlayers,
    int PlayingTimeMinutes,
    bool IsActive,
    bool IsAvailable);

public sealed record BoardGameResponse(
    Guid Id,
    string Title,
    string Publisher,
    string? Description,
    int PublicationYear,
    int MinPlayers,
    int MaxPlayers,
    int PlayingTimeMinutes,
    bool IsActive,
    int TotalCopies,
    int AvailableCopies,
    bool IsAvailable,
    IReadOnlyList<BoardGameCategoryResponse> Categories);

internal static class BoardGameResponseMappings
{
    internal static BoardGameResponse ToResponse(
        this Application.BoardGames.BoardGameDetails source) =>
        new(
            source.Id,
            source.Title,
            source.Publisher,
            source.Description,
            source.PublicationYear,
            source.MinPlayers,
            source.MaxPlayers,
            source.PlayingTimeMinutes,
            source.IsActive,
            source.TotalCopies,
            source.AvailableCopies,
            source.IsAvailable,
            source.Categories
                .Select(category => new BoardGameCategoryResponse(
                    category.Id,
                    category.Name,
                    category.IsActive))
                .ToArray());

    internal static PagedResponse<BoardGameListItemResponse> ToResponse(
        this PagedResult<Application.BoardGames.BoardGameListItem> source) =>
        new(
            source.Items
                .Select(item => new BoardGameListItemResponse(
                    item.Id,
                    item.Title,
                    item.Publisher,
                    item.PublicationYear,
                    item.MinPlayers,
                    item.MaxPlayers,
                    item.PlayingTimeMinutes,
                    item.IsActive,
                    item.IsAvailable))
                .ToArray(),
            source.Page,
            source.PageSize,
            source.TotalCount,
            source.TotalPages);
}
