using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.Loans;

namespace BoardGameLibrary.Application.Loans;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Acquires an update row lock and must be called inside an explicit transaction.
    /// The loan-return flow acquires this lock before checking and applying the return transition.
    /// </summary>
    Task<Loan?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task<LoanDetails?> GetDetailsAsync(
        GetLoanQuery query,
        CancellationToken cancellationToken);

    Task<PagedResult<LoanListItem>> ListAsync(
        ListLoansQuery query,
        CancellationToken cancellationToken);

    Task<bool> HasOpenLoanForGameCopyAsync(
        Guid gameCopyId,
        CancellationToken cancellationToken);

    Task<int> CountOpenLoansForMemberAsync(
        Guid memberId,
        CancellationToken cancellationToken);

    Task<bool> HasOverdueLoanForMemberAsync(
        Guid memberId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    void Add(Loan loan);

    void Update(Loan loan);
}
