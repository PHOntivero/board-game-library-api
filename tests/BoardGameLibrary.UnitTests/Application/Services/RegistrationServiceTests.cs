using BoardGameLibrary.Application.BoardGames;
using BoardGameLibrary.Application.Categories;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.GameCopies;
using BoardGameLibrary.Application.Loans;
using BoardGameLibrary.Application.Members;
using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Domain.GameCopies;

namespace BoardGameLibrary.UnitTests.Application.Services;

public sealed class RegistrationServiceTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CategoryList_WhenActiveFilterIsOmitted_RequestsOnlyActiveRecords()
    {
        PageRequest pageRequest = CreatePageRequest(
            CategorySortFields.Allowed,
            CategorySortFields.Default,
            CategorySortFields.DefaultDirection);
        var repository = new FakeCategoryRepository
        {
            ListResult = PagedResult<CategoryListItem>.Create([], pageRequest, 0),
        };
        var service = new CategoryService(repository, new FakeUnitOfWork());

        Result<PagedResult<CategoryListItem>> result = await service.ListAsync(
            new ListCategoriesQuery(null, null, pageRequest),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.ReceivedListQuery!.IsActive);
    }

    [Fact]
    public async Task BoardGameCreate_WhenRequestedCategoryIsInactive_ReturnsPublicConflict()
    {
        Category category = Category.Create("Strategy");
        category.SetActive(false);
        var categoryRepository = new FakeCategoryRepository
        {
            EntitiesByIds = [category],
        };
        var unitOfWork = new FakeUnitOfWork();
        var service = new BoardGameService(
            new FakeBoardGameRepository(),
            categoryRepository,
            unitOfWork,
            new CountingTimeProvider(UtcNow));

        Result<Guid> result = await service.CreateAsync(
            new CreateBoardGameCommand(
                "Brass: Birmingham",
                "Roxley",
                null,
                2018,
                2,
                4,
                120,
                [category.Id]),
            CancellationToken.None);

        Error error = Assert.Single(result.Errors);
        Assert.Equal(ErrorCodes.Categories.Inactive, error.Code);
        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task MemberCreate_WhenNormalizedMemberNumberExists_ReturnsConflictBeforeSave()
    {
        var repository = new FakeMemberRepository
        {
            DuplicateMemberNumber = true,
        };
        var unitOfWork = new FakeUnitOfWork();
        var service = new MemberService(
            repository,
            unitOfWork,
            new CountingTimeProvider(UtcNow));

        Result<Guid> result = await service.CreateAsync(
            new CreateMemberCommand(
                "  mem-001  ",
                "Ada Lovelace",
                "ada@example.com",
                null,
                new DateOnly(2026, 1, 1)),
            CancellationToken.None);

        Error error = Assert.Single(result.Errors);
        Assert.Equal(ErrorCodes.Members.DuplicateMemberNumber, error.Code);
        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GameCopyUpdate_WhenCopyHasOpenLoan_RollsBackWithoutMutationOrSave()
    {
        Guid boardGameId = Guid.CreateVersion7();
        GameCopy copy = GameCopy.Create(
            boardGameId,
            "GAME-001",
            GameCopyCondition.Good,
            null,
            DateOnly.FromDateTime(UtcNow.UtcDateTime));
        var calls = new List<string>();
        var copyRepository = new FakeGameCopyRepository(calls) { Entity = copy };
        var loanRepository = new FakeLoanRepository(calls) { HasOpenLoan = true };
        var unitOfWork = new FakeUnitOfWork(calls);
        var service = new GameCopyService(
            copyRepository,
            new FakeBoardGameRepository(),
            loanRepository,
            unitOfWork,
            new CountingTimeProvider(UtcNow));

        Result<GameCopyDetails> result = await service.UpdateAsync(
            new UpdateGameCopyCommand(
                copy.Id,
                "GAME-CHANGED",
                GameCopyCondition.Damaged,
                null,
                false),
            CancellationToken.None);

        Error error = Assert.Single(result.Errors);
        Assert.Equal(ErrorCodes.GameCopies.HasOpenLoan, error.Code);
        Assert.Equal(["begin", "copy.update", "loan.open", "rollback"], calls);
        Assert.Equal("GAME-001", copy.InventoryCode);
        Assert.Equal(GameCopyCondition.Good, copy.Condition);
        Assert.True(copy.IsActive);
        Assert.Equal(0, unitOfWork.SaveCount);
        Assert.True(unitOfWork.Transaction.WasRolledBack);
        Assert.False(unitOfWork.Transaction.WasCommitted);
    }

    [Fact]
    public async Task GameCopyUpdate_WhenCopyRemainsActive_AllowsUpdateWithoutOpenLoanCheck()
    {
        Guid boardGameId = Guid.CreateVersion7();
        GameCopy copy = GameCopy.Create(
            boardGameId,
            "GAME-001",
            GameCopyCondition.Good,
            null,
            DateOnly.FromDateTime(UtcNow.UtcDateTime));
        var calls = new List<string>();
        var copyRepository = new FakeGameCopyRepository(calls)
        {
            Entity = copy,
            HasLoanHistory = true,
            Details = new GameCopyDetails(
                copy.Id,
                boardGameId,
                "A game",
                "GAME-CHANGED",
                GameCopyCondition.Fair,
                true,
                null,
                false),
        };
        var loanRepository = new FakeLoanRepository(calls) { HasOpenLoan = true };
        var unitOfWork = new FakeUnitOfWork(calls);
        var service = new GameCopyService(
            copyRepository,
            new FakeBoardGameRepository(),
            loanRepository,
            unitOfWork,
            new CountingTimeProvider(UtcNow));

        Result<GameCopyDetails> result = await service.UpdateAsync(
            new UpdateGameCopyCommand(
                copy.Id,
                "GAME-CHANGED",
                GameCopyCondition.Fair,
                null,
                true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("GAME-CHANGED", copy.InventoryCode);
        Assert.Equal(GameCopyCondition.Fair, copy.Condition);
        Assert.True(copy.IsActive);
        Assert.DoesNotContain("loan.open", calls);
        Assert.Equal(["begin", "copy.update", "save", "commit"], calls);
        Assert.True(unitOfWork.Transaction.WasCommitted);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(null, 100)]
    [InlineData("00000000-0000-0000-0000-000000000000", null)]
    public async Task BoardGameList_WhenFilterIsInvalid_ReturnsValidationWithoutQueryingRepository(
        string? categoryId,
        int? players)
    {
        PageRequest pageRequest = CreatePageRequest(
            BoardGameSortFields.Allowed,
            BoardGameSortFields.Default,
            BoardGameSortFields.DefaultDirection);
        Guid? parsedCategoryId = categoryId is null ? null : Guid.Parse(categoryId);
        var service = new BoardGameService(
            new FakeBoardGameRepository(),
            new FakeCategoryRepository(),
            new FakeUnitOfWork(),
            new CountingTimeProvider(UtcNow));

        Result<PagedResult<BoardGameListItem>> result = await service.ListAsync(
            new ListBoardGamesQuery(
                null,
                parsedCategoryId,
                players,
                null,
                null,
                pageRequest),
            CancellationToken.None);

        Error error = Assert.Single(result.Errors);
        Assert.Equal(ErrorCodes.Common.ValidationFailed, error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);
    }

    [Fact]
    public async Task GameCopyList_WhenBoardGameFilterIsEmpty_ReturnsValidationWithoutLookup()
    {
        PageRequest pageRequest = CreatePageRequest(
            GameCopySortFields.Allowed,
            GameCopySortFields.Default,
            GameCopySortFields.DefaultDirection);
        var service = new GameCopyService(
            new FakeGameCopyRepository(),
            new FakeBoardGameRepository(),
            new FakeLoanRepository(),
            new FakeUnitOfWork(),
            new CountingTimeProvider(UtcNow));

        Result<PagedResult<GameCopyListItem>> result = await service.ListAsync(
            new ListGameCopiesQuery(Guid.Empty, null, null, null, pageRequest),
            CancellationToken.None);

        Error error = Assert.Single(result.Errors);
        Assert.Equal(ErrorCodes.Common.ValidationFailed, error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);
    }

    private static PageRequest CreatePageRequest(
        IReadOnlyCollection<string> allowed,
        string defaultField,
        SortDirection defaultDirection) =>
        PageRequest.Create(null, null, null, null, allowed, defaultField, defaultDirection).Value;
}
