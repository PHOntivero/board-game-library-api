using System.Data;

namespace BoardGameLibrary.Application.Common.Persistence;

public interface IUnitOfWork
{
    Task<ITransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
