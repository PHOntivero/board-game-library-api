using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Application.Loans;

public interface ILoanService
{
    Task<Result<Guid>> CreateAsync(
        CreateLoanCommand command,
        CancellationToken cancellationToken);

    Task<Result<LoanDetails>> GetAsync(
        GetLoanQuery query,
        CancellationToken cancellationToken);

    Task<Result<PagedResult<LoanListItem>>> ListAsync(
        ListLoansQuery query,
        CancellationToken cancellationToken);

    Task<Result> ReturnAsync(
        ReturnLoanCommand command,
        CancellationToken cancellationToken);
}
