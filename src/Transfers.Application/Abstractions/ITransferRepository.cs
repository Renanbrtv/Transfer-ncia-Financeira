using Transfers.Domain.Transfers;

namespace Transfers.Application.Abstractions;

public interface ITransferRepository
{
    /// <summary>Leitura sem rastreamento, para consultas.</summary>
    Task<Transfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Transfer?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transfer>> ListByAccountAsync(int accountId, int take, CancellationToken cancellationToken);

    /// <summary>Ids de transferências agendadas cujo horário já chegou, das mais antigas para as mais novas.</summary>
    Task<IReadOnlyList<Guid>> GetDueScheduledIdsAsync(DateTimeOffset now, int maxCount, CancellationToken cancellationToken);

    /// <summary>Carrega a transferência com lock de atualização, aguardando locks concorrentes.</summary>
    Task<Transfer?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Carrega a transferência com lock de atualização somente se ela ainda estiver Scheduled.
    /// Linhas já travadas por outra transação são ignoradas (READPAST), o que permite várias
    /// instâncias do worker sem processamento duplicado.
    /// </summary>
    Task<Transfer?> GetScheduledForProcessingAsync(Guid id, CancellationToken cancellationToken);

    void Add(Transfer transfer);
}
