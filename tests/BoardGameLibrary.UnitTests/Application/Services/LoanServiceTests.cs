using System.Data;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Loans;
using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Domain.GameCopies;
using BoardGameLibrary.Domain.Loans;
using BoardGameLibrary.Domain.Members;

namespace BoardGameLibrary.UnitTests.Application.Services;

public sealed class LoanServiceTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_WithEligibleResources_LocksInOrderAndCommitsAfterSave()
    {
        LoanFixture fixture = CreateFixture();

        Result<Guid> result = await fixture.Service.CreateAsync(
            new CreateLoanCommand(fixture.Member.Id, fixture.Copy.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.LoanRepository.Added!.Id, result.Value);
        Assert.Equal(UtcNow, fixture.LoanRepository.Added.LoanedAtUtc);
        Assert.Equal(UtcNow.AddDays(Loan.LendingTermDays), fixture.LoanRepository.Added.DueAtUtc);
        Assert.Equal(
            [
                "copy.read",
                "begin",
                "board.share",
                "member.update",
                "copy.update",
                "loan.open",
                "loan.overdue",
                "loan.count",
                "loan.add",
                "save",
                "commit",
            ],
            fixture.Calls);
        Assert.Equal(IsolationLevel.ReadCommitted, fixture.UnitOfWork.IsolationLevel);
        Assert.Equal(1, fixture.TimeProvider.ReadCount);
        Assert.True(fixture.UnitOfWork.Transaction.WasCommitted);
        Assert.False(fixture.UnitOfWork.Transaction.WasRolledBack);
    }

    [Fact]
    public async Task Create_WhenMemberHasOverdueLoan_RollsBackBeforeCountOrSave()
    {
        LoanFixture fixture = CreateFixture();
        fixture.LoanRepository.HasOverdueLoan = true;

        Result<Guid> result = await fixture.Service.CreateAsync(
            new CreateLoanCommand(fixture.Member.Id, fixture.Copy.Id),
            CancellationToken.None);

        Error error = Assert.Single(result.Errors);
        Assert.Equal(ErrorCodes.Members.HasOverdueLoan, error.Code);
        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.DoesNotContain("loan.count", fixture.Calls);
        Assert.DoesNotContain("loan.add", fixture.Calls);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
        Assert.True(fixture.UnitOfWork.Transaction.WasRolledBack);
    }

    [Fact]
    public async Task Create_WhenSaveReturnsKnownConstraintConflict_RollsBackAndPropagatesError()
    {
        LoanFixture fixture = CreateFixture();
        Error conflict = Error.Conflict(
            ErrorCodes.GameCopies.HasOpenLoan,
            "The physical copy already has an open loan.");
        fixture.UnitOfWork.SaveResult = Result<int>.Failure(conflict);

        Result<Guid> result = await fixture.Service.CreateAsync(
            new CreateLoanCommand(fixture.Member.Id, fixture.Copy.Id),
            CancellationToken.None);

        Assert.Equal(conflict, Assert.Single(result.Errors));
        Assert.True(fixture.UnitOfWork.Transaction.WasRolledBack);
        Assert.False(fixture.UnitOfWork.Transaction.WasCommitted);
        Assert.Equal("rollback", fixture.Calls[^1]);
    }

    [Fact]
    public async Task Return_WhenLoanWasAlreadyReturned_ReturnsConflictAndRollsBack()
    {
        LoanFixture fixture = CreateFixture();
        Loan returnedLoan = Loan.Create(
            fixture.Copy.Id,
            fixture.Member.Id,
            UtcNow.AddDays(-2));
        returnedLoan.Return(UtcNow.AddDays(-1));
        fixture.LoanRepository.Entity = returnedLoan;

        Result result = await fixture.Service.ReturnAsync(
            new ReturnLoanCommand(returnedLoan.Id),
            CancellationToken.None);

        Error error = Assert.Single(result.Errors);
        Assert.Equal(ErrorCodes.Loans.AlreadyReturned, error.Code);
        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal(["begin", "loan.update", "rollback"], fixture.Calls);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
        Assert.Equal(1, fixture.TimeProvider.ReadCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task List_WhenIdentifierFilterIsEmpty_ReturnsValidationWithoutReadingClock(
        bool emptyMemberId)
    {
        LoanFixture fixture = CreateFixture();
        PageRequest pageRequest = PageRequest.Create(
            null,
            null,
            null,
            null,
            LoanSortFields.Allowed,
            LoanSortFields.Default,
            LoanSortFields.DefaultDirection).Value;
        Guid? memberId = emptyMemberId ? Guid.Empty : null;
        Guid? gameCopyId = emptyMemberId ? null : Guid.Empty;

        Result<PagedResult<LoanListItem>> result = await fixture.Service.ListAsync(
            new ListLoansQuery(memberId, gameCopyId, null, null, null, pageRequest),
            CancellationToken.None);

        Error error = Assert.Single(result.Errors);
        Assert.Equal(ErrorCodes.Common.ValidationFailed, error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(0, fixture.TimeProvider.ReadCount);
    }

    private static LoanFixture CreateFixture()
    {
        var calls = new List<string>();
        DateOnly todayUtc = DateOnly.FromDateTime(UtcNow.UtcDateTime);
        Category category = Category.Create("Strategy");
        BoardGame boardGame = BoardGame.Create(
            "Brass: Birmingham",
            "Roxley",
            null,
            2018,
            2,
            4,
            120,
            [category],
            todayUtc);
        GameCopy copy = GameCopy.Create(
            boardGame.Id,
            "BRASS-001",
            GameCopyCondition.Good,
            null,
            todayUtc);
        Member member = Member.Create(
            "MEM-001",
            "Ada Lovelace",
            "ada@example.com",
            null,
            todayUtc,
            todayUtc);
        var loanRepository = new FakeLoanRepository(calls);
        var boardGameRepository = new FakeBoardGameRepository(calls)
        {
            Entity = boardGame,
            SharedEntity = boardGame,
        };
        var memberRepository = new FakeMemberRepository(calls) { Entity = member };
        var copyRepository = new FakeGameCopyRepository(calls) { Entity = copy };
        var unitOfWork = new FakeUnitOfWork(calls);
        var timeProvider = new CountingTimeProvider(UtcNow);
        var service = new LoanService(
            loanRepository,
            boardGameRepository,
            memberRepository,
            copyRepository,
            unitOfWork,
            timeProvider);

        return new LoanFixture(
            service,
            loanRepository,
            unitOfWork,
            timeProvider,
            copy,
            member,
            calls);
    }

    private sealed record LoanFixture(
        LoanService Service,
        FakeLoanRepository LoanRepository,
        FakeUnitOfWork UnitOfWork,
        CountingTimeProvider TimeProvider,
        GameCopy Copy,
        Member Member,
        List<string> Calls);
}
