using Microsoft.Extensions.Logging;
using Transfers.Application.Abstractions;
using Transfers.Application.Exceptions;
using Transfers.Domain.Accounts;
using Transfers.Domain.Limits;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

/// <summary>
/// Fluxo de execução compartilhado pela transferência imediata e pelo processamento de agendamentos,
/// garantindo que as duas passem exatamente pelas mesmas regras.
/// </summary>
public sealed class TransferExecutor(
    IAccountRepository accounts,
    ITransferAttemptRepository attempts,
    TransferLimitPolicy limitPolicy,
    TimeProvider timeProvider,
    ILogger<TransferExecutor> logger)
{
    /// <summary>
    /// Trava origem e destino com UPDLOCK sempre na mesma ordem (menor Id primeiro).
    /// A ordem fixa evita deadlock entre transferências cruzadas (A→B e B→A ao mesmo tempo).
    /// </summary>
    public async Task<(Account Source, Account Destination)> LockAccountsAsync(
        int sourceAccountId,
        int destinationAccountId,
        CancellationToken cancellationToken)
    {
        var firstId = Math.Min(sourceAccountId, destinationAccountId);
        var secondId = Math.Max(sourceAccountId, destinationAccountId);

        var first = await accounts.GetForUpdateAsync(firstId, cancellationToken)
            ?? throw new NotFoundException("Conta", firstId);
        var second = await accounts.GetForUpdateAsync(secondId, cancellationToken)
            ?? throw new NotFoundException("Conta", secondId);

        return first.Id == sourceAccountId ? (first, second) : (second, first);
    }

    /// <summary>
    /// Avalia as regras e aplica o resultado na transferência e nas contas, registrando a tentativa.
    /// Pré-condição: as contas foram obtidas por <see cref="LockAccountsAsync"/> na transação corrente.
    /// </summary>
    public async Task ExecuteAsync(Transfer transfer, Account source, Account destination, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // Com a conta de origem travada, nenhuma outra transferência dela pode gravar tentativas
        // entre esta leitura e o commit: a soma da hora e a contagem ficam consistentes.
        var usage = await attempts.GetUsageSinceAsync(source.Id, TransferLimitPolicy.WindowStart(now), cancellationToken);
        var limit = limitPolicy.GetLimitAt(now);

        var attempt = transfer.Execute(source, destination, usage, limit, now);
        attempts.Add(attempt);

        if (transfer.Status == TransferStatus.Completed)
        {
            logger.LogInformation(
                "Transferência {TransferId} concluída: {Amount} da conta {SourceAccountId} para {DestinationAccountId}.",
                transfer.Id, transfer.Amount, transfer.SourceAccountId, transfer.DestinationAccountId);
        }
        else
        {
            logger.LogWarning(
                "Transferência {TransferId} rejeitada ({Reason}). Período {Period}, uso na hora: {Attempts} tentativas / {TransferredAmount}.",
                transfer.Id, transfer.FailureReason, limitPolicy.GetPeriodAt(now), usage.Attempts, usage.TransferredAmount);
        }
    }
}
