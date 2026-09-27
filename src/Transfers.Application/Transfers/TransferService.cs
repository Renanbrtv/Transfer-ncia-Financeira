using Microsoft.Extensions.Logging;
using Transfers.Application.Abstractions;
using Transfers.Application.Exceptions;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public sealed class TransferService(
    IAccountRepository accounts,
    ITransferRepository transfers,
    IUnitOfWork unitOfWork,
    TransferExecutor executor,
    TimeProvider timeProvider,
    ILogger<TransferService> logger)
{
    /// <summary>
    /// Transferência imediata. Tudo acontece em uma única transação: lock das contas, validação das regras,
    /// débito/crédito e registro da transferência e da tentativa. Rejeições por regra de negócio também são
    /// confirmadas (status Failed), pois precisam contar como tentativa.
    /// </summary>
    public async Task<TransferOperationResult> TransferAsync(
        CreateTransferRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var transfer = Transfer.CreateImmediate(
            request.SourceAccountId,
            request.DestinationAccountId,
            request.Amount,
            timeProvider.GetUtcNow(),
            idempotencyKey);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var (source, destination) = await executor.LockAccountsAsync(
            transfer.SourceAccountId, transfer.DestinationAccountId, cancellationToken);

        // Verificada depois do lock: duas requisições com a mesma chave (e, portanto, a mesma origem)
        // ficam serializadas, e a segunda enxerga a transferência gravada pela primeira.
        if (await FindPreviousAsync(transfer, cancellationToken) is { } previous)
        {
            return TransferOperationResult.Replayed(TransferResponse.From(previous));
        }

        transfers.Add(transfer);
        await executor.ExecuteAsync(transfer, source, destination, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return TransferOperationResult.Created(TransferResponse.From(transfer));
    }

    /// <summary>
    /// Agendamento: valida os dados e a existência das contas. Saldo, status e limites são verificados
    /// somente na execução, pelo mesmo fluxo da transferência imediata.
    /// </summary>
    public async Task<TransferOperationResult> ScheduleAsync(
        ScheduleTransferRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var transfer = Transfer.Schedule(
            request.SourceAccountId,
            request.DestinationAccountId,
            request.Amount,
            request.ScheduledFor,
            timeProvider.GetUtcNow(),
            idempotencyKey);

        await EnsureAccountExistsAsync(transfer.SourceAccountId, cancellationToken);
        await EnsureAccountExistsAsync(transfer.DestinationAccountId, cancellationToken);

        // Sem lock aqui: se duas requisições com a mesma chave chegarem juntas, o índice único
        // de IdempotencyKey barra a segunda (a API responde 409).
        if (await FindPreviousAsync(transfer, cancellationToken) is { } previous)
        {
            return TransferOperationResult.Replayed(TransferResponse.From(previous));
        }

        transfers.Add(transfer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Transferência {TransferId} agendada para {ScheduledFor}: {Amount} da conta {SourceAccountId} para {DestinationAccountId}.",
            transfer.Id, transfer.ScheduledFor, transfer.Amount, transfer.SourceAccountId, transfer.DestinationAccountId);

        return TransferOperationResult.Created(TransferResponse.From(transfer));
    }

    /// <summary>
    /// Cancela uma transferência agendada. O lock na linha impede que o cancelamento e o worker
    /// atuem ao mesmo tempo sobre a mesma transferência.
    /// </summary>
    public async Task<TransferResponse> CancelAsync(Guid transferId, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var transfer = await transfers.GetForUpdateAsync(transferId, cancellationToken)
            ?? throw new NotFoundException("Transferência", transferId);

        transfer.Cancel(timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Transferência {TransferId} cancelada.", transfer.Id);

        return TransferResponse.From(transfer);
    }

    public async Task<TransferResponse> GetByIdAsync(Guid transferId, CancellationToken cancellationToken)
    {
        var transfer = await transfers.GetByIdAsync(transferId, cancellationToken)
            ?? throw new NotFoundException("Transferência", transferId);

        return TransferResponse.From(transfer);
    }

    private async Task<Transfer?> FindPreviousAsync(Transfer transfer, CancellationToken cancellationToken)
    {
        if (transfer.IdempotencyKey is null)
        {
            return null;
        }

        var previous = await transfers.GetByIdempotencyKeyAsync(transfer.IdempotencyKey, cancellationToken);
        if (previous is null)
        {
            return null;
        }

        if (!previous.HasSameContentAs(transfer))
        {
            throw new IdempotencyConflictException(transfer.IdempotencyKey);
        }

        logger.LogInformation(
            "Requisição repetida com a chave de idempotência {IdempotencyKey}; devolvendo a transferência {TransferId}.",
            transfer.IdempotencyKey, previous.Id);

        return previous;
    }

    private async Task EnsureAccountExistsAsync(int accountId, CancellationToken cancellationToken)
    {
        if (!await accounts.ExistsAsync(accountId, cancellationToken))
        {
            throw new NotFoundException("Conta", accountId);
        }
    }
}
