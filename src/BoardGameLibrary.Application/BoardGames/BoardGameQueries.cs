using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Application.BoardGames;

public sealed record GetBoardGameQuery(Guid Id);

public sealed record ListBoardGamesQuery(
    string? Search,
    Guid? CategoryId,
    int? Players,
    bool? IsAvailable,
    bool? IsActive,
    PageRequest PageRequest);

public static class BoardGameSortFields
{
    public const string Title = "title";
    public const string Publisher = "publisher";
    public const string PublicationYear = "publicationYear";
    public const string MinPlayers = "minPlayers";
    public const string MaxPlayers = "maxPlayers";
    public const string PlayingTimeMinutes = "playingTimeMinutes";

    public const string Default = Title;

    public static IReadOnlyCollection<string> Allowed { get; } = Array.AsReadOnly(
        [Title, Publisher, PublicationYear, MinPlayers, MaxPlayers, PlayingTimeMinutes]);

    public const SortDirection DefaultDirection = SortDirection.Ascending;
}
