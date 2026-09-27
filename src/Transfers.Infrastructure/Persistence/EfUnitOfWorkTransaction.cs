using Microsoft.EntityFrameworkCore.Storage;
using Transfers.Application.Abstractions;

namespace Transfers.Infrastructure.Persistence;

internal sealed class EfUnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

    // Sem commit, o Dispose do EF faz o rollback.
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
