using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Domain.Limits;
using Transfers.Domain.Transfers;
using Transfers.Infrastructure.Persistence;

namespace Transfers.Infrastructure.Repositories;

internal sealed class TransferAttemptRepository(TransfersDbContext dbContext) : ITransferAttemptRepository
{
    public async Task<HourlyUsage> GetUsageSinceAsync(int accountId, DateTimeOffset since, CancellationToken cancellationToken)
    {
        var window = dbContext.TransferAttempts
            .Where(attempt => attempt.AccountId == accountId && attempt.AttemptedAt > since);

        var attempts = await window.CountAsync(cancellationToken);
        var transferredAmount = await window
            .Where(attempt => attempt.Succeeded)
            .SumAsync(attempt => attempt.Amount, cancellationToken);

        return new HourlyUsage(attempts, transferredAmount);
    }

    public void Add(TransferAttempt attempt) => dbContext.TransferAttempts.Add(attempt);
}
