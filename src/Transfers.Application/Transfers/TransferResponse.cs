using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

/// <summary>Dados da transferência.</summary>
/// <param name="Id">Identificador da transferência.</param>
/// <param name="SourceAccountId">Conta de origem.</param>
/// <param name="DestinationAccountId">Conta de destino.</param>
/// <param name="Amount">Valor.</param>
/// <param name="Type">Immediate ou Scheduled.</param>
/// <param name="Status">Scheduled, Processing, Completed, Failed ou Cancelled.</param>
/// <param name="CreatedAt">Quando a transferência foi registrada (UTC).</param>
/// <param name="ScheduledFor">Quando deve ser executada (somente agendadas).</param>
/// <param name="ProcessedAt">Quando foi concluída ou falhou.</param>
/// <param name="CancelledAt">Quando foi cancelada.</param>
/// <param name="FailureCode">Código do motivo da falha (ex.: InsufficientFunds).</param>
/// <param name="FailureMessage">Descrição legível do motivo da falha.</param>
public sealed record TransferResponse(
    Guid Id,
    int SourceAccountId,
    int DestinationAccountId,
    decimal Amount,
    TransferType Type,
    TransferStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledFor,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset? CancelledAt,
    string? FailureCode,
    string? FailureMessage)
{
    public static TransferResponse From(Transfer transfer) =>
        new(
            transfer.Id,
            transfer.SourceAccountId,
            transfer.DestinationAccountId,
            transfer.Amount,
            transfer.Type,
            transfer.Status,
            transfer.CreatedAt,
            transfer.ScheduledFor,
            transfer.ProcessedAt,
            transfer.CancelledAt,
            transfer.FailureReason?.ToString(),
            transfer.FailureReason?.ToMessage());
}
