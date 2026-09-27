namespace Transfers.Domain.Transfers;

/// <summary>
/// Registro de cada execução de transferência feita a partir de uma conta, bem-sucedida ou não.
/// É a base para os limites por hora (quantidade de tentativas e valor transferido).
/// </summary>
public sealed class TransferAttempt
{
    // Construtor usado pelo EF Core na materialização.
    private TransferAttempt()
    {
    }

    private TransferAttempt(
        int accountId,
        Guid transferId,
        decimal amount,
        bool succeeded,
        TransferRejectionReason? rejectionReason,
        DateTimeOffset attemptedAt)
    {
        AccountId = accountId;
        TransferId = transferId;
        Amount = amount;
        Succeeded = succeeded;
        RejectionReason = rejectionReason;
        AttemptedAt = attemptedAt;
    }

    public long Id { get; private set; }

    /// <summary>Conta de origem, dona da tentativa.</summary>
    public int AccountId { get; private set; }

    public Guid TransferId { get; private set; }

    public decimal Amount { get; private set; }

    public bool Succeeded { get; private set; }

    public TransferRejectionReason? RejectionReason { get; private set; }

    public DateTimeOffset AttemptedAt { get; private set; }

    internal static TransferAttempt ForSuccess(Transfer transfer, DateTimeOffset attemptedAt) =>
        new(transfer.SourceAccountId, transfer.Id, transfer.Amount, succeeded: true, rejectionReason: null, attemptedAt);

    internal static TransferAttempt ForRejection(Transfer transfer, TransferRejectionReason reason, DateTimeOffset attemptedAt) =>
        new(transfer.SourceAccountId, transfer.Id, transfer.Amount, succeeded: false, reason, attemptedAt);
}
