namespace Transfers.Domain.Transfers;

/// <summary>
/// Motivos pelos quais uma transferência válida é rejeitada no momento da execução.
/// Toda rejeição fica registrada na transferência (status Failed) e conta como tentativa.
/// </summary>
public enum TransferRejectionReason
{
    SourceAccountNotActive = 1,
    DestinationAccountNotActive = 2,
    HourlyAttemptLimitExceeded = 3,
    HourlyAmountLimitExceeded = 4,
    InsufficientFunds = 5,
    AccountNotFound = 6
}

public static class TransferRejectionReasonExtensions
{
    public static string ToMessage(this TransferRejectionReason reason) => reason switch
    {
        TransferRejectionReason.SourceAccountNotActive => "A conta de origem não está ativa.",
        TransferRejectionReason.DestinationAccountNotActive => "A conta de destino não está ativa.",
        TransferRejectionReason.HourlyAttemptLimitExceeded => "Limite de tentativas de transferência por hora excedido.",
        TransferRejectionReason.HourlyAmountLimitExceeded => "Limite de valor transferido por hora excedido.",
        TransferRejectionReason.InsufficientFunds => "Saldo + cheque especial insuficiente.",
        TransferRejectionReason.AccountNotFound => "Conta de origem ou destino não encontrada no momento da execução.",
        _ => reason.ToString()
    };
}
