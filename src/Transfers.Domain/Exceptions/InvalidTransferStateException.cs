using Transfers.Domain.Transfers;

namespace Transfers.Domain.Exceptions;

/// <summary>
/// Tentativa de uma transição de estado não permitida (ex.: cancelar uma transferência já concluída).
/// </summary>
public sealed class InvalidTransferStateException(Guid transferId, TransferStatus currentStatus, string operation)
    : DomainException(
        "transfer.invalid_state",
        $"Não é possível {operation} a transferência {transferId}: o status atual é {currentStatus}.")
{
    public Guid TransferId { get; } = transferId;

    public TransferStatus CurrentStatus { get; } = currentStatus;
}
