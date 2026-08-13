using BoardGameLibrary.Application.BoardGames;
using BoardGameLibrary.Application.Categories;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Common.Persistence;
using BoardGameLibrary.Application.GameCopies;
using BoardGameLibrary.Application.Loans;
using BoardGameLibrary.Application.Members;
using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Domain.GameCopies;
using BoardGameLibrary.Domain.Loans;
using BoardGameLibrary.Domain.Members;
using BoardGameLibrary.Infrastructure.Persistence;
using BoardGameLibrary.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BoardGameLibrary.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class PersistenceRepositoryTests(PostgreSqlFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task LoanList_WithMemberFilter_ExecutesServerSide()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        DateOnly todayUtc = DateOnly.FromDateTime(utcNow.UtcDateTime);
        Category category = Category.Create("Repository translation");
        BoardGame boardGame = BoardGame.Create(
            "Repository translation game",
            "Integration Publisher",
            null,
            2020,
            1,
            4,
            60,
            [category],
            todayUtc);
        GameCopy copy = GameCopy.Create(
            boardGame.Id,
            "REPOSITORY-001",
            GameCopyCondition.Good,
            todayUtc,
            todayUtc);
        Member member = Member.Create(
            "REPOSITORY-001",
            "Repository Member",
            "repository@example.test",
            null,
            todayUtc,
            todayUtc);
        Loan loan = Loan.Create(copy.Id, member.Id, utcNow);

        await Fixture.InDatabaseScopeAsync(async dbContext =>
        {
            dbContext.BoardGames.Add(boardGame);
            dbContext.GameCopies.Add(copy);
            dbContext.Members.Add(member);
            dbContext.Loans.Add(loan);
            await dbContext.SaveChangesAsync(cancellationToken);
        });

        await using AsyncServiceScope scope = Fixture.Factory.Services.CreateAsyncScope();
        ILoanService service = scope.ServiceProvider.GetRequiredService<ILoanService>();
        PageRequest pageRequest = PageRequest.Create(
            1,
            20,
            LoanSortFields.Default,
            "desc",
            LoanSortFields.Allowed,
            LoanSortFields.Default,
            LoanSortFields.DefaultDirection).Value;

        Result<PagedResult<LoanListItem>> result = await service.ListAsync(
            new ListLoansQuery(member.Id, null, null, null, null, pageRequest),
            cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(loan.Id, result.Value.Items.Single().Id);
    }

    [Fact]
    public async Task AddedGameCopy_WithMissingBoardGame_MapsToBoardGameNotFound()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateOnly todayUtc = UtcToday();
        GameCopy copy = GameCopy.Create(
            Guid.NewGuid(),
            "MISSING-BOARD-GAME",
            GameCopyCondition.Good,
            todayUtc,
            todayUtc);

        Result<int> result = await SaveWithUnitOfWorkAsync(
            dbContext =>
            {
                dbContext.GameCopies.Add(copy);
                return Task.CompletedTask;
            },
            cancellationToken);

        AssertFailure(result, ErrorCodes.BoardGames.NotFound, ErrorType.NotFound);
    }

    [Fact]
    public async Task DeletedBoardGame_WithCopy_MapsToHasCopies()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        PersistenceGraph graph = await PersistGraphAsync(
            includeMember: false,
            includeLoan: false,
            cancellationToken: cancellationToken);

        Result<int> result = await SaveWithUnitOfWorkAsync(
            async dbContext =>
            {
                BoardGame boardGame = await dbContext.BoardGames.SingleAsync(
                    item => item.Id == graph.BoardGameId,
                    cancellationToken);
                dbContext.BoardGames.Remove(boardGame);
            },
            cancellationToken);

        AssertFailure(result, ErrorCodes.BoardGames.HasCopies, ErrorType.Conflict);
    }

    [Fact]
    public async Task AddedBoardGameJoin_WithMissingCategory_MapsToCategoryNotFound()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateOnly todayUtc = UtcToday();
        Category missingCategory = Category.Create("Missing category");
        BoardGame boardGame = BoardGame.Create(
            "Missing category game",
            "Integration Publisher",
            null,
            2020,
            1,
            4,
            60,
            [missingCategory],
            todayUtc);

        Result<int> result = await SaveWithUnitOfWorkAsync(
            dbContext =>
            {
                dbContext.Categories.Attach(missingCategory);
                dbContext.BoardGames.Add(boardGame);
                return Task.CompletedTask;
            },
            cancellationToken);

        AssertFailure(result, ErrorCodes.Categories.NotFound, ErrorType.NotFound);
    }

    [Fact]
    public async Task DeletedCategory_WithBoardGame_MapsToHasBoardGames()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        PersistenceGraph graph = await PersistGraphAsync(
            includeCopy: false,
            includeMember: false,
            includeLoan: false,
            cancellationToken: cancellationToken);

        Result<int> result = await SaveWithUnitOfWorkAsync(
            async dbContext =>
            {
                Category category = await dbContext.Categories.SingleAsync(
                    item => item.Id == graph.CategoryId,
                    cancellationToken);
                dbContext.Categories.Remove(category);
            },
            cancellationToken);

        AssertFailure(result, ErrorCodes.Categories.HasBoardGames, ErrorType.Conflict);
    }

    [Fact]
    public async Task AddedLoan_WithMissingGameCopy_MapsToGameCopyNotFound()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        PersistenceGraph graph = await PersistGraphAsync(
            includeCopy: false,
            includeLoan: false,
            cancellationToken: cancellationToken);
        Loan loan = Loan.Create(Guid.NewGuid(), graph.MemberId, DateTimeOffset.UtcNow);

        Result<int> result = await SaveWithUnitOfWorkAsync(
            dbContext =>
            {
                dbContext.Loans.Add(loan);
                return Task.CompletedTask;
            },
            cancellationToken);

        AssertFailure(result, ErrorCodes.GameCopies.NotFound, ErrorType.NotFound);
    }

    [Fact]
    public async Task DeletedGameCopy_WithLoan_MapsToHasLoanHistory()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        PersistenceGraph graph = await PersistGraphAsync(cancellationToken: cancellationToken);

        Result<int> result = await SaveWithUnitOfWorkAsync(
            async dbContext =>
            {
                GameCopy copy = await dbContext.GameCopies.SingleAsync(
                    item => item.Id == graph.GameCopyId,
                    cancellationToken);
                dbContext.GameCopies.Remove(copy);
            },
            cancellationToken);

        AssertFailure(result, ErrorCodes.GameCopies.HasLoanHistory, ErrorType.Conflict);
    }

    [Fact]
    public async Task AddedLoan_WithMissingMember_MapsToMemberNotFound()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        PersistenceGraph graph = await PersistGraphAsync(
            includeMember: false,
            includeLoan: false,
            cancellationToken: cancellationToken);
        Loan loan = Loan.Create(graph.GameCopyId, Guid.NewGuid(), DateTimeOffset.UtcNow);

        Result<int> result = await SaveWithUnitOfWorkAsync(
            dbContext =>
            {
                dbContext.Loans.Add(loan);
                return Task.CompletedTask;
            },
            cancellationToken);

        AssertFailure(result, ErrorCodes.Members.NotFound, ErrorType.NotFound);
    }

    [Fact]
    public async Task DeletedMember_WithLoan_MapsToHasLoanHistory()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        PersistenceGraph graph = await PersistGraphAsync(cancellationToken: cancellationToken);

        Result<int> result = await SaveWithUnitOfWorkAsync(
            async dbContext =>
            {
                Member member = await dbContext.Members.SingleAsync(
                    item => item.Id == graph.MemberId,
                    cancellationToken);
                dbContext.Members.Remove(member);
            },
            cancellationToken);

        AssertFailure(result, ErrorCodes.Members.HasLoanHistory, ErrorType.Conflict);
    }

    [Fact]
    public async Task UnsupportedForeignKeyContext_IsRethrown()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateOnly todayUtc = UtcToday();
        Category category = Category.Create("Unsupported FK category");
        BoardGame missingBoardGame = BoardGame.Create(
            "Unsupported FK game",
            "Integration Publisher",
            null,
            2020,
            1,
            4,
            60,
            [category],
            todayUtc);

        await Assert.ThrowsAsync<DbUpdateException>(() => SaveWithUnitOfWorkAsync(
            dbContext =>
            {
                dbContext.Categories.Add(category);
                dbContext.BoardGames.Attach(missingBoardGame);
                return Task.CompletedTask;
            },
            cancellationToken));
    }

    [Fact]
    public async Task ActiveFilters_MustBeNormalizedByApplicationServices()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AsyncServiceScope scope = Fixture.Factory.Services.CreateAsyncScope();
        var boardGames = scope.ServiceProvider.GetRequiredService<IBoardGameRepository>();
        var categories = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var gameCopies = scope.ServiceProvider.GetRequiredService<IGameCopyRepository>();
        var members = scope.ServiceProvider.GetRequiredService<IMemberRepository>();

        PageRequest boardGamePage = CreatePage(
            BoardGameSortFields.Allowed,
            BoardGameSortFields.Default,
            BoardGameSortFields.DefaultDirection);
        PageRequest categoryPage = CreatePage(
            CategorySortFields.Allowed,
            CategorySortFields.Default,
            CategorySortFields.DefaultDirection);
        PageRequest gameCopyPage = CreatePage(
            GameCopySortFields.Allowed,
            GameCopySortFields.Default,
            GameCopySortFields.DefaultDirection);
        PageRequest memberPage = CreatePage(
            MemberSortFields.Allowed,
            MemberSortFields.Default,
            MemberSortFields.DefaultDirection);

        await Assert.ThrowsAsync<InvalidOperationException>(() => boardGames.ListAsync(
            new ListBoardGamesQuery(null, null, null, null, null, boardGamePage),
            cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => categories.ListAsync(
            new ListCategoriesQuery(null, null, categoryPage),
            cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => gameCopies.ListByBoardGameAsync(
            new ListGameCopiesQuery(Guid.NewGuid(), null, null, null, gameCopyPage),
            cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => members.ListAsync(
            new ListMembersQuery(null, null, memberPage),
            cancellationToken));
    }

    private async Task<PersistenceGraph> PersistGraphAsync(
        bool includeCopy = true,
        bool includeMember = true,
        bool includeLoan = true,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        DateOnly todayUtc = DateOnly.FromDateTime(utcNow.UtcDateTime);
        Category category = Category.Create("Constraint category");
        BoardGame boardGame = BoardGame.Create(
            "Constraint game",
            "Integration Publisher",
            null,
            2020,
            1,
            4,
            60,
            [category],
            todayUtc);
        GameCopy copy = GameCopy.Create(
            boardGame.Id,
            "CONSTRAINT-001",
            GameCopyCondition.Good,
            todayUtc,
            todayUtc);
        Member member = Member.Create(
            "CONSTRAINT-001",
            "Constraint Member",
            "constraint@example.test",
            null,
            todayUtc,
            todayUtc);

        await Fixture.InDatabaseScopeAsync(async dbContext =>
        {
            dbContext.BoardGames.Add(boardGame);

            if (includeCopy)
            {
                dbContext.GameCopies.Add(copy);
            }

            if (includeMember)
            {
                dbContext.Members.Add(member);
            }

            if (includeLoan)
            {
                dbContext.Loans.Add(Loan.Create(copy.Id, member.Id, utcNow));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        });

        return new PersistenceGraph(category.Id, boardGame.Id, copy.Id, member.Id);
    }

    private async Task<Result<int>> SaveWithUnitOfWorkAsync(
        Func<BoardGameLibraryDbContext, Task> arrange,
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = Fixture.Factory.Services.CreateAsyncScope();
        BoardGameLibraryDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<BoardGameLibraryDbContext>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await arrange(dbContext);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void AssertFailure(
        Result<int> result,
        string expectedCode,
        ErrorType expectedType)
    {
        Assert.True(result.IsFailure);
        Error error = Assert.Single(result.Errors);
        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(expectedType, error.Type);
    }

    private static DateOnly UtcToday() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static PageRequest CreatePage(
        IReadOnlyCollection<string> allowedSortFields,
        string defaultSortBy,
        SortDirection defaultDirection) =>
        PageRequest.Create(
            1,
            20,
            defaultSortBy,
            null,
            allowedSortFields,
            defaultSortBy,
            defaultDirection).Value;

    private sealed record PersistenceGraph(
        Guid CategoryId,
        Guid BoardGameId,
        Guid GameCopyId,
        Guid MemberId);
}
