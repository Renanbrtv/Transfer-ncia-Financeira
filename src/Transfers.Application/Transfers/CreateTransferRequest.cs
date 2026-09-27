namespace Transfers.Application.Transfers;

/// <summary>Pedido de transferência imediata.</summary>
public sealed record CreateTransferRequest
{
    /// <summary>Conta de origem.</summary>
    /// <example>1</example>
    public required int SourceAccountId { get; init; }

    /// <summary>Conta de destino.</summary>
    /// <example>2</example>
    public required int DestinationAccountId { get; init; }

    /// <summary>Valor em reais, maior que zero e com até duas casas decimais.</summary>
    /// <example>150.00</example>
    public required decimal Amount { get; init; }
}
