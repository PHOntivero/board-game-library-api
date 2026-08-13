using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Domain.Common;
using BoardGameLibrary.Domain.Loans;
using BoardGameLibrary.Infrastructure;
using BoardGameLibrary.Infrastructure.Persistence;
using BoardGameLibrary.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BoardGameLibrary.IntegrationTests.Persistence;

[Collection(IntegrationTestCollection.Name)]
public sealed class DemoDataSeederTests(PostgreSqlFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string ManuallyChangedPublisher = "Manually changed publisher";

    [Fact]
    public async Task EmptyDatabase_SeedsExpectedDataset()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await RunDemoSeedAsync(cancellationToken);

        SeedCounts counts = await LoadCountsAsync(cancellationToken);

        Assert.Equal(
            new SeedCounts(
                Categories: 17,
                BoardGames: 120,
                GameCopies: 180,
                Members: 30,
                Loans: 50,
                ReturnedLoans: 30,
                ActiveLoans: 10,
                OverdueLoans: 10),
            counts);
    }

    [Fact]
    public async Task Rerun_IsIdempotent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await RunDemoSeedAsync(cancellationToken);
        SeedSnapshot before = await LoadSnapshotAsync(cancellationToken);

        await RunDemoSeedAsync(cancellationToken);
        SeedSnapshot after = await LoadSnapshotAsync(cancellationToken);

        Assert.Equal(before.CategoryIds, after.CategoryIds);
        Assert.Equal(before.BoardGameIds, after.BoardGameIds);
        Assert.Equal(before.GameCopyIds, after.GameCopyIds);
        Assert.Equal(before.MemberIds, after.MemberIds);
        Assert.Equal(before.Loans, after.Loans);
    }

    [Fact]
    public async Task Rerun_PreservesManualChanges()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await RunDemoSeedAsync(cancellationToken);

        int affectedRows = await Fixture.InDatabaseScopeAsync(dbContext =>
            dbContext.BoardGames
                .Where(boardGame => boardGame.Title == "Catan")
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        boardGame => boardGame.Publisher,
                        ManuallyChangedPublisher),
                    cancellationToken));

        Assert.Equal(1, affectedRows);

        await RunDemoSeedAsync(cancellationToken);

        string publisher = await Fixture.InDatabaseScopeAsync(dbContext =>
            dbContext.BoardGames
                .AsNoTracking()
                .Where(boardGame => boardGame.Title == "Catan")
                .Select(boardGame => boardGame.Publisher)
                .SingleAsync(cancellationToken));

        Assert.Equal(ManuallyChangedPublisher, publisher);
    }

    [Fact]
    public async Task Rerun_DoesNotAddOpenLoanForMemberWithOverdueLoan()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await RunDemoSeedAsync(cancellationToken);

        Guid removedActiveLoanId = await Fixture.InDatabaseScopeAsync(async dbContext =>
        {
            Guid memberId = await dbContext.Members
                .Where(member => member.MemberNumber == "MEM-011")
                .Select(member => member.Id)
                .SingleAsync(cancellationToken);
            Loan activeLoan = await dbContext.Loans
                .SingleAsync(
                    loan => loan.MemberId == memberId && loan.ReturnedAtUtc == null,
                    cancellationToken);
            Guid freeCopyId = await dbContext.GameCopies
                .Where(copy => copy.InventoryCode == "CATAN-001")
                .Select(copy => copy.Id)
                .SingleAsync(cancellationToken);

            dbContext.Loans.Remove(activeLoan);
            dbContext.Loans.Add(Loan.Create(
                freeCopyId,
                memberId,
                DateTimeOffset.UtcNow.AddDays(-30)));
            await dbContext.SaveChangesAsync(cancellationToken);

            return activeLoan.Id;
        });

        await RunDemoSeedAsync(cancellationToken);

        bool removedLoanWasRestored = await Fixture.InDatabaseScopeAsync(dbContext =>
            dbContext.Loans
                .AsNoTracking()
                .AnyAsync(loan => loan.Id == removedActiveLoanId, cancellationToken));

        Assert.False(removedLoanWasRestored);
    }

    [Fact]
    public async Task Failure_RollsBackAllSeedStages()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await Fixture.InDatabaseScopeAsync(async dbContext =>
        {
            Category inactiveStrategy = Category.Create("Strategy");
            inactiveStrategy.SetActive(false);
            dbContext.Categories.Add(inactiveStrategy);
            await dbContext.SaveChangesAsync(cancellationToken);
        });

        DomainException exception = await Assert.ThrowsAsync<DomainException>(
            () => RunDemoSeedAsync(cancellationToken));
        SeedCounts counts = await LoadCountsAsync(cancellationToken);

        Assert.Equal("inactive_category", exception.Code);
        Assert.Equal(1, counts.Categories);
        Assert.Equal(0, counts.BoardGames);
        Assert.Equal(0, counts.GameCopies);
        Assert.Equal(0, counts.Members);
        Assert.Equal(0, counts.Loans);
    }

    private async Task RunDemoSeedAsync(CancellationToken cancellationToken)
    {
        string connectionString = await Fixture.InDatabaseScopeAsync(dbContext =>
            Task.FromResult(
                dbContext.Database.GetConnectionString()
                ?? throw new InvalidOperationException("The test database has no connection string.")));
        var services = new ServiceCollection();
        services.AddInfrastructure(connectionString, enableDemoSeed: true);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        BoardGameLibraryDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<BoardGameLibraryDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private Task<SeedCounts> LoadCountsAsync(CancellationToken cancellationToken) =>
        Fixture.InDatabaseScopeAsync(async dbContext =>
        {
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;

            return new SeedCounts(
                await dbContext.Categories.CountAsync(cancellationToken),
                await dbContext.BoardGames.CountAsync(cancellationToken),
                await dbContext.GameCopies.CountAsync(cancellationToken),
                await dbContext.Members.CountAsync(cancellationToken),
                await dbContext.Loans.CountAsync(cancellationToken),
                await dbContext.Loans.CountAsync(
                    loan => loan.ReturnedAtUtc != null,
                    cancellationToken),
                await dbContext.Loans.CountAsync(
                    loan => loan.ReturnedAtUtc == null && utcNow <= loan.DueAtUtc,
                    cancellationToken),
                await dbContext.Loans.CountAsync(
                    loan => loan.ReturnedAtUtc == null && utcNow > loan.DueAtUtc,
                    cancellationToken));
        });

    private Task<SeedSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken) =>
        Fixture.InDatabaseScopeAsync(async dbContext =>
        {
            Guid[] categoryIds = await dbContext.Categories
                .AsNoTracking()
                .OrderBy(category => category.Id)
                .Select(category => category.Id)
                .ToArrayAsync(cancellationToken);
            Guid[] boardGameIds = await dbContext.BoardGames
                .AsNoTracking()
                .OrderBy(boardGame => boardGame.Id)
                .Select(boardGame => boardGame.Id)
                .ToArrayAsync(cancellationToken);
            Guid[] gameCopyIds = await dbContext.GameCopies
                .AsNoTracking()
                .OrderBy(copy => copy.Id)
                .Select(copy => copy.Id)
                .ToArrayAsync(cancellationToken);
            Guid[] memberIds = await dbContext.Members
                .AsNoTracking()
                .OrderBy(member => member.Id)
                .Select(member => member.Id)
                .ToArrayAsync(cancellationToken);
            LoanSnapshot[] loans = await dbContext.Loans
                .AsNoTracking()
                .OrderBy(loan => loan.Id)
                .Select(loan => new LoanSnapshot(
                    loan.Id,
                    loan.GameCopyId,
                    loan.MemberId,
                    loan.LoanedAtUtc,
                    loan.DueAtUtc,
                    loan.ReturnedAtUtc))
                .ToArrayAsync(cancellationToken);

            return new SeedSnapshot(categoryIds, boardGameIds, gameCopyIds, memberIds, loans);
        });

    private sealed record SeedCounts(
        int Categories,
        int BoardGames,
        int GameCopies,
        int Members,
        int Loans,
        int ReturnedLoans,
        int ActiveLoans,
        int OverdueLoans);

    private sealed record SeedSnapshot(
        Guid[] CategoryIds,
        Guid[] BoardGameIds,
        Guid[] GameCopyIds,
        Guid[] MemberIds,
        LoanSnapshot[] Loans);

    private sealed record LoanSnapshot(
        Guid Id,
        Guid GameCopyId,
        Guid MemberId,
        DateTimeOffset LoanedAtUtc,
        DateTimeOffset DueAtUtc,
        DateTimeOffset? ReturnedAtUtc);
}
