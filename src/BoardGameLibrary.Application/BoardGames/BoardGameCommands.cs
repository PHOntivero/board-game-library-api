namespace BoardGameLibrary.Application.BoardGames;

public sealed record CreateBoardGameCommand(
    string Title,
    string Publisher,
    string? Description,
    int PublicationYear,
    int MinPlayers,
    int MaxPlayers,
    int PlayingTimeMinutes,
    IReadOnlyCollection<Guid> CategoryIds);

public sealed record UpdateBoardGameCommand(
    Guid Id,
    string Title,
    string Publisher,
    string? Description,
    int PublicationYear,
    int MinPlayers,
    int MaxPlayers,
    int PlayingTimeMinutes,
    IReadOnlyCollection<Guid> CategoryIds,
    bool IsActive);

public sealed record DeleteBoardGameCommand(Guid Id);
