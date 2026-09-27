using Microsoft.Extensions.Logging;
using Transfers.Application.Abstractions;
using Transfers.Application.Exceptions;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

/// <summary>
/// Executa transferências agendadas que venceram. Cada transferência é processada em sua própria
/// transação; se o processo cair no meio, o rollback a devolve para Scheduled e ela é retomada depois.
/// </summary>
public sealed class ScheduledTransferProcessor(
    ITransferRepository transfers,
    IUnitOfWork unitOfWork,
    TransferExecutor executor,
    TimeProvider timeProvider,
    ILogger<ScheduledTransferProcessor> logger)
{
    public Task<IReadOnlyList<Guid>> GetDueTransferIdsAsync(int maxCount, CancellationToken cancellationToken) =>
        transfers.GetDueScheduledIdsAsync(timeProvider.GetUtcNow(), maxCount, cancellationToken);

    /// <returns>True se a transferência foi processada; false se não estava mais disponível.</returns>
    public async Task<bool> ProcessAsync(Guid transferId, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var transfer = await transfers.GetScheduledForProcessingAsync(transferId, cancellationToken);
        if (transfer is null)
        {
            // Cancelada, já processada ou travada por outra instância do worker neste momento.
            logger.LogDebug("Transferência agendada {TransferId} não está disponível para processamento.", transferId);
            return false;
        }

        transfer.StartProcessing(timeProvider.GetUtcNow());

        try
        {
            var (source, destination) = await executor.LockAccountsAsync(
                transfer.SourceAccountId, transfer.DestinationAccountId, cancellationToken);

            await executor.ExecuteAsync(transfer, source, destination, cancellationToken);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning(ex, "Conta não encontrada ao executar a transferência agendada {TransferId}.", transfer.Id);
            transfer.Fail(TransferRejectionReason.AccountNotFound, timeProvider.GetUtcNow());
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Transferência agendada {TransferId} processada com status {Status}.", transfer.Id, transfer.Status);

        return true;
    }
}
