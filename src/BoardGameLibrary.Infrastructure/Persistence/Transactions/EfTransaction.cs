using BoardGameLibrary.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace BoardGameLibrary.Infrastructure.Persistence.Transactions;

internal sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction
{
    private readonly IDbContextTransaction _transaction = transaction;

    public Task CommitAsync(CancellationToken cancellationToken) =>
        _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken) =>
        _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
