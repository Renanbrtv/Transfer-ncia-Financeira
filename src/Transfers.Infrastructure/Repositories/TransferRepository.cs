using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Domain.Transfers;
using Transfers.Infrastructure.Persistence;

namespace Transfers.Infrastructure.Repositories;

internal sealed class TransferRepository(TransfersDbContext dbContext) : ITransferRepository
{
    public Task<Transfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Transfers.AsNoTracking().SingleOrDefaultAsync(transfer => transfer.Id == id, cancellationToken);

    public Task<Transfer?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        dbContext.Transfers.AsNoTracking()
            .SingleOrDefaultAsync(transfer => transfer.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<IReadOnlyList<Transfer>> ListByAccountAsync(int accountId, int take, CancellationToken cancellationToken) =>
        await dbContext.Transfers.AsNoTracking()
            .Where(transfer => transfer.SourceAccountId == accountId || transfer.DestinationAccountId == accountId)
            .OrderByDescending(transfer => transfer.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetDueScheduledIdsAsync(DateTimeOffset now, int maxCount, CancellationToken cancellationToken) =>
        await dbContext.Transfers.AsNoTracking()
            .Where(transfer => transfer.Status == TransferStatus.Scheduled && transfer.ScheduledFor <= now)
            .OrderBy(transfer => transfer.ScheduledFor)
            .Select(transfer => transfer.Id)
            .Take(maxCount)
            .ToListAsync(cancellationToken);

    public Task<Transfer?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Transfers
            .FromSql($"SELECT * FROM [Transfers] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Transfer?> GetScheduledForProcessingAsync(Guid id, CancellationToken cancellationToken)
    {
        var scheduled = nameof(TransferStatus.Scheduled);

        // READPAST: se a linha estiver travada (outra instância do worker ou um cancelamento em andamento),
        // ela é simplesmente pulada em vez de esperar o lock.
        return dbContext.Transfers
            .FromSql($"SELECT * FROM [Transfers] WITH (UPDLOCK, ROWLOCK, READPAST) WHERE [Id] = {id} AND [Status] = {scheduled}")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public void Add(Transfer transfer) => dbContext.Transfers.Add(transfer);
}
