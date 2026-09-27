using Transfers.Domain.Accounts;
using Transfers.Domain.Exceptions;
using Transfers.Domain.Limits;

namespace Transfers.Domain.Transfers;

/// <summary>
/// Transferência entre duas contas. Concentra a máquina de estados e as regras de execução;
/// a camada de aplicação só precisa buscar os dados (contas travadas e uso na última hora) e persistir.
/// </summary>
public sealed class Transfer
{
    public const int IdempotencyKeyMaxLength = 100;

    // Construtor usado pelo EF Core na materialização.
    private Transfer()
    {
    }

    private Transfer(
        int sourceAccountId,
        int destinationAccountId,
        decimal amount,
        TransferType type,
        TransferStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? scheduledFor,
        string? idempotencyKey)
    {
        Id = Guid.NewGuid();
        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        Type = type;
        Status = status;
        CreatedAt = createdAt;
        ScheduledFor = scheduledFor;
        IdempotencyKey = idempotencyKey;
    }

    public Guid Id { get; private set; }

    public int SourceAccountId { get; private set; }

    public int DestinationAccountId { get; private set; }

    public decimal Amount { get; private set; }

    public TransferType Type { get; private set; }

    public TransferStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ScheduledFor { get; private set; }

    /// <summary>Momento em que a transferência foi concluída ou falhou.</summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public TransferRejectionReason? FailureReason { get; private set; }

    public string? IdempotencyKey { get; private set; }

    /// <summary>Token de concorrência otimista (rowversion do SQL Server).</summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Transferência imediata: nasce em Processing e é executada na mesma operação.</summary>
    public static Transfer CreateImmediate(
        int sourceAccountId,
        int destinationAccountId,
        decimal amount,
        DateTimeOffset now,
        string? idempotencyKey = null)
    {
        ValidateRequest(sourceAccountId, destinationAccountId, amount, idempotencyKey);

        return new Transfer(
            sourceAccountId, destinationAccountId, amount,
            TransferType.Immediate, TransferStatus.Processing,
            now, scheduledFor: null, idempotencyKey);
    }

    /// <summary>
    /// Transferência agendada: nasce em Scheduled. Saldo, status e limites só são avaliados na execução.
    /// </summary>
    public static Transfer Schedule(
        int sourceAccountId,
        int destinationAccountId,
        decimal amount,
        DateTimeOffset scheduledFor,
        DateTimeOffset now,
        string? idempotencyKey = null)
    {
        ValidateRequest(sourceAccountId, destinationAccountId, amount, idempotencyKey);

        if (scheduledFor <= now)
        {
            throw new DomainValidationException("transfer.schedule_not_in_future", "A data/hora do agendamento deve ser futura.");
        }

        return new Transfer(
            sourceAccountId, destinationAccountId, amount,
            TransferType.Scheduled, TransferStatus.Scheduled,
            now, scheduledFor, idempotencyKey);
    }

    public void StartProcessing(DateTimeOffset now)
    {
        EnsureStatus(TransferStatus.Scheduled, "processar");

        if (ScheduledFor > now)
        {
            throw new DomainException("transfer.not_due", $"A transferência {Id} está agendada para {ScheduledFor:O} e ainda não pode ser executada.");
        }

        Status = TransferStatus.Processing;
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsureStatus(TransferStatus.Scheduled, "cancelar");

        Status = TransferStatus.Cancelled;
        CancelledAt = now;
    }

    /// <summary>
    /// Executa a transferência: avalia as regras e, se todas passarem, debita a origem e credita o destino.
    /// Uma rejeição não lança exceção: a transferência vai para Failed e a tentativa é devolvida
    /// para ser persistida, pois tentativas rejeitadas também contam para o limite.
    /// </summary>
    /// <remarks>As contas precisam estar travadas pelo chamador durante toda a operação.</remarks>
    public TransferAttempt Execute(
        Account source,
        Account destination,
        HourlyUsage usage,
        PeriodLimit limit,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(limit);
        EnsureStatus(TransferStatus.Processing, "executar");

        if (source.Id != SourceAccountId || destination.Id != DestinationAccountId)
        {
            throw new InvalidOperationException($"As contas informadas não correspondem à transferência {Id}.");
        }

        if (FindRejection(source, destination, usage, limit) is { } reason)
        {
            MarkAsFailed(reason, now);
            return TransferAttempt.ForRejection(this, reason, now);
        }

        source.Debit(Amount);
        destination.Credit(Amount);

        Status = TransferStatus.Completed;
        ProcessedAt = now;

        return TransferAttempt.ForSuccess(this, now);
    }

    /// <summary>Falha sem avaliar regras (ex.: conta removida antes da execução de um agendamento).</summary>
    public void Fail(TransferRejectionReason reason, DateTimeOffset now)
    {
        EnsureStatus(TransferStatus.Processing, "marcar como falha");
        MarkAsFailed(reason, now);
    }

    /// <summary>Usado pela idempotência: a mesma chave só pode ser reutilizada com o mesmo conteúdo.</summary>
    public bool HasSameContentAs(Transfer other) =>
        SourceAccountId == other.SourceAccountId
        && DestinationAccountId == other.DestinationAccountId
        && Amount == other.Amount
        && Type == other.Type
        && ScheduledFor == other.ScheduledFor;

    // A ordem importa: as regras de status vêm primeiro porque são as mais informativas;
    // saldo por último porque é a única que depende do valor atual da conta.
    private TransferRejectionReason? FindRejection(Account source, Account destination, HourlyUsage usage, PeriodLimit limit)
    {
        if (!source.IsActive)
        {
            return TransferRejectionReason.SourceAccountNotActive;
        }

        if (!destination.IsActive)
        {
            return TransferRejectionReason.DestinationAccountNotActive;
        }

        if (usage.Attempts >= limit.MaxAttemptsPerHour)
        {
            return TransferRejectionReason.HourlyAttemptLimitExceeded;
        }

        if (usage.TransferredAmount + Amount > limit.MaxAmountPerHour)
        {
            return TransferRejectionReason.HourlyAmountLimitExceeded;
        }

        if (!source.CanCover(Amount))
        {
            return TransferRejectionReason.InsufficientFunds;
        }

        return null;
    }

    private void MarkAsFailed(TransferRejectionReason reason, DateTimeOffset now)
    {
        Status = TransferStatus.Failed;
        FailureReason = reason;
        ProcessedAt = now;
    }

    private void EnsureStatus(TransferStatus expected, string operation)
    {
        if (Status != expected)
        {
            throw new InvalidTransferStateException(Id, Status, operation);
        }
    }

    private static void ValidateRequest(int sourceAccountId, int destinationAccountId, decimal amount, string? idempotencyKey)
    {
        if (sourceAccountId <= 0 || destinationAccountId <= 0)
        {
            throw new DomainValidationException("transfer.invalid_account", "Os identificadores das contas devem ser positivos.");
        }

        if (sourceAccountId == destinationAccountId)
        {
            throw new DomainValidationException("transfer.same_account", "A conta de origem deve ser diferente da conta de destino.");
        }

        Money.EnsureValidAmount(amount);

        if (idempotencyKey is not null && (idempotencyKey.Length == 0 || idempotencyKey.Length > IdempotencyKeyMaxLength))
        {
            throw new DomainValidationException(
                "transfer.invalid_idempotency_key",
                $"A chave de idempotência deve ter entre 1 e {IdempotencyKeyMaxLength} caracteres.");
        }
    }
}
