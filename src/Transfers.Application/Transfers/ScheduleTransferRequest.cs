namespace Transfers.Application.Transfers;

/// <summary>Pedido de transferência agendada.</summary>
public sealed record ScheduleTransferRequest
{
    /// <summary>Conta de origem.</summary>
    /// <example>1</example>
    public required int SourceAccountId { get; init; }

    /// <summary>Conta de destino.</summary>
    /// <example>2</example>
    public required int DestinationAccountId { get; init; }

    /// <summary>Valor em reais, maior que zero e com até duas casas decimais.</summary>
    /// <example>250.00</example>
    public required decimal Amount { get; init; }

    /// <summary>Data/hora futura da execução, em ISO 8601 com fuso (ex.: 2030-01-15T14:30:00-03:00).</summary>
    /// <example>2030-01-15T14:30:00-03:00</example>
    public required DateTimeOffset ScheduledFor { get; init; }
}
