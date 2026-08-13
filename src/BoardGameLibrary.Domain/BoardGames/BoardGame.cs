using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.Domain.BoardGames;

public sealed class BoardGame
{
    public const int TitleMaximumLength = 200;
    public const int PublisherMaximumLength = 120;
    public const int DescriptionMaximumLength = 2_000;
    public const int MinimumPublicationYear = 1900;
    public const int MinimumPlayers = 1;
    public const int MaximumPlayers = 99;
    public const int MinimumPlayingTimeMinutes = 1;
    public const int MaximumPlayingTimeMinutes = 1_440;

    private readonly List<Category> _categories = [];

    private BoardGame()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Publisher { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int PublicationYear { get; private set; }

    public int MinPlayers { get; private set; }

    public int MaxPlayers { get; private set; }

    public int PlayingTimeMinutes { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<Category> Categories => _categories.AsReadOnly();

    public static BoardGame Create(
        string title,
        string publisher,
        string? description,
        int publicationYear,
        int minPlayers,
        int maxPlayers,
        int playingTimeMinutes,
        IEnumerable<Category> categories,
        DateOnly todayUtc)
    {
        ValidatedDetails details = ValidateDetails(
            title,
            publisher,
            description,
            publicationYear,
            minPlayers,
            maxPlayers,
            playingTimeMinutes,
            todayUtc);
        List<Category> validatedCategories = ValidateCategories(categories, new HashSet<Guid>());

        var boardGame = new BoardGame
        {
            Id = Guid.CreateVersion7(),
            Title = details.Title,
            Publisher = details.Publisher,
            Description = details.Description,
            PublicationYear = publicationYear,
            MinPlayers = minPlayers,
            MaxPlayers = maxPlayers,
            PlayingTimeMinutes = playingTimeMinutes,
            IsActive = true,
        };

        boardGame._categories.AddRange(validatedCategories);
        return boardGame;
    }

    public void Update(
        string title,
        string publisher,
        string? description,
        int publicationYear,
        int minPlayers,
        int maxPlayers,
        int playingTimeMinutes,
        IEnumerable<Category> categories,
        DateOnly todayUtc)
    {
        ValidatedDetails details = ValidateDetails(
            title,
            publisher,
            description,
            publicationYear,
            minPlayers,
            maxPlayers,
            playingTimeMinutes,
            todayUtc);
        HashSet<Guid> existingCategoryIds = _categories.Select(category => category.Id).ToHashSet();
        List<Category> validatedCategories = ValidateCategories(categories, existingCategoryIds);

        Title = details.Title;
        Publisher = details.Publisher;
        Description = details.Description;
        PublicationYear = publicationYear;
        MinPlayers = minPlayers;
        MaxPlayers = maxPlayers;
        PlayingTimeMinutes = playingTimeMinutes;
        _categories.Clear();
        _categories.AddRange(validatedCategories);
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    private static ValidatedDetails ValidateDetails(
        string? title,
        string? publisher,
        string? description,
        int publicationYear,
        int minPlayers,
        int maxPlayers,
        int playingTimeMinutes,
        DateOnly todayUtc)
    {
        string normalizedTitle = DomainGuard.RequiredText(
            title,
            TitleMaximumLength,
            "Board game title",
            "board_game.title");
        string normalizedPublisher = DomainGuard.RequiredText(
            publisher,
            PublisherMaximumLength,
            "Board game publisher",
            "board_game.publisher");
        string? normalizedDescription = DomainGuard.OptionalText(
            description,
            DescriptionMaximumLength,
            "Board game description",
            "board_game.description");

        if (publicationYear < MinimumPublicationYear || publicationYear > todayUtc.Year + 1)
        {
            throw new DomainException(
                "board_game.publication_year_out_of_range",
                $"Publication year must be between {MinimumPublicationYear} and {todayUtc.Year + 1}.",
                DomainErrorType.Validation);
        }

        if (minPlayers is < MinimumPlayers or > MaximumPlayers)
        {
            throw new DomainException(
                "board_game.min_players_out_of_range",
                $"Minimum players must be between {MinimumPlayers} and {MaximumPlayers}.",
                DomainErrorType.Validation);
        }

        if (maxPlayers is < MinimumPlayers or > MaximumPlayers)
        {
            throw new DomainException(
                "board_game.max_players_out_of_range",
                $"Maximum players must be between {MinimumPlayers} and {MaximumPlayers}.",
                DomainErrorType.Validation);
        }

        if (maxPlayers < minPlayers)
        {
            throw new DomainException(
                "board_game.player_range_invalid",
                "Maximum players must be greater than or equal to minimum players.",
                DomainErrorType.Validation);
        }

        if (playingTimeMinutes is < MinimumPlayingTimeMinutes or > MaximumPlayingTimeMinutes)
        {
            throw new DomainException(
                "board_game.playing_time_out_of_range",
                $"Playing time must be between {MinimumPlayingTimeMinutes} and {MaximumPlayingTimeMinutes} minutes.",
                DomainErrorType.Validation);
        }

        return new ValidatedDetails(normalizedTitle, normalizedPublisher, normalizedDescription);
    }

    private static List<Category> ValidateCategories(
        IEnumerable<Category>? categories,
        IReadOnlySet<Guid> existingCategoryIds)
    {
        if (categories is null)
        {
            throw new DomainException(
                "board_game.categories_required",
                "A board game must have at least one category.",
                DomainErrorType.Validation);
        }

        List<Category> categoryList = categories.ToList();

        if (categoryList.Count == 0)
        {
            throw new DomainException(
                "board_game.categories_required",
                "A board game must have at least one category.",
                DomainErrorType.Validation);
        }

        if (categoryList.Any(category => category is null))
        {
            throw new DomainException(
                "board_game.category_invalid",
                "A board game category cannot be null.",
                DomainErrorType.Validation);
        }

        if (categoryList.Select(category => category.Id).Distinct().Count() != categoryList.Count)
        {
            throw new DomainException(
                "board_game.categories_duplicate",
                "A board game cannot contain the same category more than once.",
                DomainErrorType.Validation);
        }

        if (categoryList.Any(category => !category.IsActive && !existingCategoryIds.Contains(category.Id)))
        {
            throw new DomainException(
                "inactive_category",
                "Inactive categories cannot be newly associated with a board game.",
                DomainErrorType.Conflict);
        }

        return categoryList;
    }

    private sealed record ValidatedDetails(string Title, string Publisher, string? Description);
}
