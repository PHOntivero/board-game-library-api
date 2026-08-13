namespace BoardGameLibrary.Application.BoardGames;

public sealed record BoardGameCategory(
    Guid Id,
    string Name,
    bool IsActive);

public sealed record BoardGameListItem(
    Guid Id,
    string Title,
    string Publisher,
    int PublicationYear,
    int MinPlayers,
    int MaxPlayers,
    int PlayingTimeMinutes,
    bool IsActive,
    bool IsAvailable);

public sealed record BoardGameDetails(
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
    IReadOnlyList<BoardGameCategory> Categories);
