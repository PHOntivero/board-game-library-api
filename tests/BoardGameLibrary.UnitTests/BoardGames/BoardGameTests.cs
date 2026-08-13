using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.UnitTests.BoardGames;

public sealed class BoardGameTests
{
    private static readonly DateOnly TodayUtc = new(2026, 8, 13);

    [Fact]
    public void Create_WithValidData_NormalizesFieldsAndCreatesActiveVersionSevenIdentifier()
    {
        Category category = Category.Create("Strategy");

        BoardGame boardGame = CreateBoardGame(
            category,
            title: "  Brass: Birmingham  ",
            publisher: "  Roxley  ",
            description: "  Economic strategy game  ");

        Assert.Equal("Brass: Birmingham", boardGame.Title);
        Assert.Equal("Roxley", boardGame.Publisher);
        Assert.Equal("Economic strategy game", boardGame.Description);
        Assert.Equal(2018, boardGame.PublicationYear);
        Assert.Equal(2, boardGame.MinPlayers);
        Assert.Equal(4, boardGame.MaxPlayers);
        Assert.Equal(120, boardGame.PlayingTimeMinutes);
        Assert.True(boardGame.IsActive);
        Assert.Same(category, Assert.Single(boardGame.Categories));
        DomainTestAssertions.VersionIsSeven(boardGame.Id);
    }

    [Fact]
    public void Create_WhenDescriptionIsWhitespace_StoresNull()
    {
        BoardGame boardGame = CreateBoardGame(Category.Create("Strategy"), description: "   ");

        Assert.Null(boardGame.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenTitleIsMissing_Throws(string? title)
    {
        DomainTestAssertions.Throws(
            "board_game.title.required",
            () => CreateBoardGame(Category.Create("Strategy"), title: title!));
    }

    [Fact]
    public void Create_WhenTextExceedsLimits_Throws()
    {
        Category category = Category.Create("Strategy");

        DomainTestAssertions.Throws(
            "board_game.title.too_long",
            () => CreateBoardGame(category, title: new string('a', BoardGame.TitleMaximumLength + 1)));
        DomainTestAssertions.Throws(
            "board_game.publisher.too_long",
            () => CreateBoardGame(category, publisher: new string('a', BoardGame.PublisherMaximumLength + 1)));
        DomainTestAssertions.Throws(
            "board_game.description.too_long",
            () => CreateBoardGame(category, description: new string('a', BoardGame.DescriptionMaximumLength + 1)));
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2028)]
    public void Create_WhenPublicationYearIsOutsideAllowedRange_Throws(int publicationYear)
    {
        DomainTestAssertions.Throws(
            "board_game.publication_year_out_of_range",
            () => CreateBoardGame(Category.Create("Strategy"), publicationYear: publicationYear));
    }

    [Theory]
    [InlineData(0, 1, "board_game.min_players_out_of_range")]
    [InlineData(100, 100, "board_game.min_players_out_of_range")]
    [InlineData(1, 0, "board_game.max_players_out_of_range")]
    [InlineData(1, 100, "board_game.max_players_out_of_range")]
    [InlineData(4, 3, "board_game.player_range_invalid")]
    public void Create_WhenPlayerRangeIsInvalid_Throws(int minPlayers, int maxPlayers, string code)
    {
        DomainTestAssertions.Throws(
            code,
            () => CreateBoardGame(
                Category.Create("Strategy"),
                minPlayers: minPlayers,
                maxPlayers: maxPlayers));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1441)]
    public void Create_WhenPlayingTimeIsOutsideAllowedRange_Throws(int playingTimeMinutes)
    {
        DomainTestAssertions.Throws(
            "board_game.playing_time_out_of_range",
            () => CreateBoardGame(Category.Create("Strategy"), playingTimeMinutes: playingTimeMinutes));
    }

    [Fact]
    public void Create_WhenCategoriesAreEmpty_Throws()
    {
        DomainTestAssertions.Throws(
            "board_game.categories_required",
            () => BoardGame.Create(
                "Brass: Birmingham",
                "Roxley",
                null,
                2018,
                2,
                4,
                120,
                [],
                TodayUtc));
    }

    [Fact]
    public void Create_WhenCategoryIsRepeated_Throws()
    {
        Category category = Category.Create("Strategy");

        DomainTestAssertions.Throws(
            "board_game.categories_duplicate",
            () => BoardGame.Create(
                "Brass: Birmingham",
                "Roxley",
                null,
                2018,
                2,
                4,
                120,
                [category, category],
                TodayUtc));
    }

    [Fact]
    public void Create_WhenCategoryIsInactive_Throws()
    {
        Category category = Category.Create("Strategy");
        category.SetActive(false);

        DomainTestAssertions.Throws(
            "inactive_category",
            () => CreateBoardGame(category),
            DomainErrorType.Conflict);
    }

    [Fact]
    public void Update_ReplacesDetailsAndCategories()
    {
        Category strategy = Category.Create("Strategy");
        Category economic = Category.Create("Economic");
        BoardGame boardGame = CreateBoardGame(strategy);

        boardGame.Update(
            "  Azul  ",
            "  Next Move  ",
            "  Abstract drafting game  ",
            2017,
            2,
            4,
            45,
            [economic],
            TodayUtc);

        Assert.Equal("Azul", boardGame.Title);
        Assert.Equal("Next Move", boardGame.Publisher);
        Assert.Equal("Abstract drafting game", boardGame.Description);
        Assert.Equal(2017, boardGame.PublicationYear);
        Assert.Equal(45, boardGame.PlayingTimeMinutes);
        Assert.Same(economic, Assert.Single(boardGame.Categories));
    }

    [Fact]
    public void Update_CanRetainExistingCategoryAfterItBecomesInactive()
    {
        Category category = Category.Create("Strategy");
        BoardGame boardGame = CreateBoardGame(category);
        category.SetActive(false);

        boardGame.Update(
            boardGame.Title,
            boardGame.Publisher,
            boardGame.Description,
            boardGame.PublicationYear,
            boardGame.MinPlayers,
            boardGame.MaxPlayers,
            boardGame.PlayingTimeMinutes,
            [category],
            TodayUtc);

        Assert.Same(category, Assert.Single(boardGame.Categories));
    }

    [Fact]
    public void Update_CannotAddAnInactiveCategory()
    {
        Category existing = Category.Create("Strategy");
        Category newCategory = Category.Create("Family");
        BoardGame boardGame = CreateBoardGame(existing);
        newCategory.SetActive(false);

        DomainTestAssertions.Throws(
            "inactive_category",
            () => boardGame.Update(
                boardGame.Title,
                boardGame.Publisher,
                boardGame.Description,
                boardGame.PublicationYear,
                boardGame.MinPlayers,
                boardGame.MaxPlayers,
                boardGame.PlayingTimeMinutes,
                [existing, newCategory],
                TodayUtc),
            DomainErrorType.Conflict);
    }

    [Fact]
    public void Categories_CannotBeMutatedThroughExposedCollection()
    {
        Category category = Category.Create("Strategy");
        BoardGame boardGame = CreateBoardGame(category);
        ICollection<Category> exposedCategories =
            Assert.IsAssignableFrom<ICollection<Category>>(boardGame.Categories);

        Assert.False(boardGame.Categories is List<Category>);
        Assert.True(exposedCategories.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => exposedCategories.Add(Category.Create("Family")));
        Assert.Same(category, Assert.Single(boardGame.Categories));
    }

    [Fact]
    public void Update_WhenValidationFails_DoesNotPartiallyChangeState()
    {
        Category category = Category.Create("Strategy");
        BoardGame boardGame = CreateBoardGame(category);

        DomainTestAssertions.Throws(
            "board_game.player_range_invalid",
            () => boardGame.Update(
                "Changed title",
                "Changed publisher",
                null,
                2020,
                5,
                2,
                90,
                [category],
                TodayUtc));

        Assert.Equal("Brass: Birmingham", boardGame.Title);
        Assert.Equal("Roxley", boardGame.Publisher);
    }

    [Fact]
    public void SetActive_ChangesActiveState()
    {
        BoardGame boardGame = CreateBoardGame(Category.Create("Strategy"));

        boardGame.SetActive(false);
        Assert.False(boardGame.IsActive);

        boardGame.SetActive(true);
        Assert.True(boardGame.IsActive);
    }

    private static BoardGame CreateBoardGame(
        Category category,
        string title = "Brass: Birmingham",
        string publisher = "Roxley",
        string? description = "Economic strategy game",
        int publicationYear = 2018,
        int minPlayers = 2,
        int maxPlayers = 4,
        int playingTimeMinutes = 120) =>
        BoardGame.Create(
            title,
            publisher,
            description,
            publicationYear,
            minPlayers,
            maxPlayers,
            playingTimeMinutes,
            [category],
            TodayUtc);
}
