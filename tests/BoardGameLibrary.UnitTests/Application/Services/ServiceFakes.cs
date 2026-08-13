using System.Data;
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

namespace BoardGameLibrary.UnitTests.Application.Services;

internal sealed class CountingTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    internal int ReadCount { get; private set; }

    public override DateTimeOffset GetUtcNow()
    {
        ReadCount++;
        return utcNow;
    }
}

internal sealed class FakeTransaction(IList<string>? calls = null) : ITransaction
{
    internal bool WasCommitted { get; private set; }

    internal bool WasRolledBack { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        WasCommitted = true;
        calls?.Add("commit");
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        WasRolledBack = true;
        calls?.Add("rollback");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class FakeUnitOfWork(IList<string>? calls = null) : IUnitOfWork
{
    internal Result<int> SaveResult { get; set; } = Result<int>.Success(1);

    internal FakeTransaction Transaction { get; } = new(calls);

    internal int SaveCount { get; private set; }

    internal IsolationLevel? IsolationLevel { get; private set; }

    public Task<ITransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        IsolationLevel = isolationLevel;
        calls?.Add("begin");
        return Task.FromResult<ITransaction>(Transaction);
    }

    public Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        calls?.Add("save");
        return Task.FromResult(SaveResult);
    }
}

internal sealed class FakeCategoryRepository : ICategoryRepository
{
    internal Category? Entity { get; set; }

    internal CategoryDetails? Details { get; set; }

    internal IReadOnlyList<Category> EntitiesByIds { get; set; } = [];

    internal PagedResult<CategoryListItem>? ListResult { get; set; }

    internal ListCategoriesQuery? ReceivedListQuery { get; private set; }

    internal bool DuplicateName { get; set; }

    internal bool HasBoardGames { get; set; }

    internal Category? Added { get; private set; }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Entity);

    public Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        Task.FromResult(EntitiesByIds);

    public Task<CategoryDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Details);

    public Task<PagedResult<CategoryListItem>> ListAsync(
        ListCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        ReceivedListQuery = query;
        return Task.FromResult(ListResult!);
    }

    public Task<bool> ExistsWithNormalizedNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        Task.FromResult(DuplicateName);

    public Task<bool> HasBoardGamesAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(HasBoardGames);

    public void Add(Category category) => Added = category;

    public void Update(Category category)
    {
    }

    public void Remove(Category category)
    {
    }
}

internal sealed class FakeBoardGameRepository(IList<string>? calls = null) : IBoardGameRepository
{
    internal BoardGame? Entity { get; set; }

    internal BoardGame? SharedEntity { get; set; }

    internal BoardGameDetails? Details { get; set; }

    internal bool HasCopies { get; set; }

    internal BoardGame? Added { get; private set; }

    public Task<BoardGame?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        calls?.Add("board.update");
        return Task.FromResult(Entity);
    }

    public Task<BoardGame?> GetByIdForShareAsync(Guid id, CancellationToken cancellationToken)
    {
        calls?.Add("board.share");
        return Task.FromResult(SharedEntity ?? Entity);
    }

    public Task<BoardGameDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Details);

    public Task<PagedResult<BoardGameListItem>> ListAsync(
        ListBoardGamesQuery query,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> HasCopiesAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(HasCopies);

    public void Add(BoardGame boardGame) => Added = boardGame;

    public void Update(BoardGame boardGame)
    {
    }

    public void Remove(BoardGame boardGame)
    {
    }
}

internal sealed class FakeMemberRepository(IList<string>? calls = null) : IMemberRepository
{
    internal Member? Entity { get; set; }

    internal bool DuplicateMemberNumber { get; set; }

    internal bool DuplicateEmail { get; set; }

    public Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Entity);

    public Task<Member?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        calls?.Add("member.update");
        return Task.FromResult(Entity);
    }

    public Task<MemberDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<PagedResult<MemberListItem>> ListAsync(
        ListMembersQuery query,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> ExistsWithNormalizedMemberNumberAsync(
        string normalizedMemberNumber,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        Task.FromResult(DuplicateMemberNumber);

    public Task<bool> ExistsWithNormalizedEmailAsync(
        string normalizedEmail,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        Task.FromResult(DuplicateEmail);

    public Task<bool> HasLoanHistoryAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public void Add(Member member)
    {
    }

    public void Update(Member member)
    {
    }

    public void Remove(Member member)
    {
    }
}

internal sealed class FakeGameCopyRepository(IList<string>? calls = null) : IGameCopyRepository
{
    internal GameCopy? Entity { get; set; }

    internal GameCopyDetails? Details { get; set; }

    internal bool DuplicateInventoryCode { get; set; }

    internal bool HasLoanHistory { get; set; }

    public Task<GameCopy?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        calls?.Add("copy.read");
        return Task.FromResult(Entity);
    }

    public Task<GameCopy?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        calls?.Add("copy.update");
        return Task.FromResult(Entity);
    }

    public Task<GameCopyDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Details);

    public Task<PagedResult<GameCopyListItem>> ListByBoardGameAsync(
        ListGameCopiesQuery query,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> ExistsWithNormalizedInventoryCodeAsync(
        string normalizedInventoryCode,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        Task.FromResult(DuplicateInventoryCode);

    public Task<bool> HasLoanHistoryAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(HasLoanHistory);

    public void Add(GameCopy gameCopy)
    {
    }

    public void Update(GameCopy gameCopy)
    {
    }

    public void Remove(GameCopy gameCopy)
    {
    }
}

internal sealed class FakeLoanRepository(IList<string>? calls = null) : ILoanRepository
{
    internal Loan? Entity { get; set; }

    internal bool HasOpenLoan { get; set; }

    internal bool HasOverdueLoan { get; set; }

    internal int OpenLoanCount { get; set; }

    internal Loan? Added { get; private set; }

    public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Entity);

    public Task<Loan?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        calls?.Add("loan.update");
        return Task.FromResult(Entity);
    }

    public Task<LoanDetails?> GetDetailsAsync(
        Guid id,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<PagedResult<LoanListItem>> ListAsync(
        ListLoansQuery query,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> HasOpenLoanForGameCopyAsync(
        Guid gameCopyId,
        CancellationToken cancellationToken)
    {
        calls?.Add("loan.open");
        return Task.FromResult(HasOpenLoan);
    }

    public Task<int> CountOpenLoansForMemberAsync(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        calls?.Add("loan.count");
        return Task.FromResult(OpenLoanCount);
    }

    public Task<bool> HasOverdueLoanForMemberAsync(
        Guid memberId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        calls?.Add("loan.overdue");
        return Task.FromResult(HasOverdueLoan);
    }

    public void Add(Loan loan)
    {
        Added = loan;
        calls?.Add("loan.add");
    }

    public void Update(Loan loan)
    {
    }
}
