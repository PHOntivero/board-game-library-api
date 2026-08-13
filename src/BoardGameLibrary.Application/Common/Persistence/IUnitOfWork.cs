using System.Data;
using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Application.Common.Persistence;

public interface IUnitOfWork
{
    Task<ITransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken);

    Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken);
}
