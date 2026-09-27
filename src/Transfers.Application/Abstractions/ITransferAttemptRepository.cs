using Transfers.Domain.Limits;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Abstractions;

public interface ITransferAttemptRepository
{
    /// <summary>Tentativas e valor concluído da conta de origem a partir de <paramref name="since"/> (exclusivo).</summary>
    Task<HourlyUsage> GetUsageSinceAsync(int accountId, DateTimeOffset since, CancellationToken cancellationToken);

    void Add(TransferAttempt attempt);
}
