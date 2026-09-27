namespace Transfers.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>Abre uma transação READ COMMITTED. Se não for confirmada, é desfeita no Dispose.</summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
