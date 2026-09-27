namespace Transfers.Application.Exceptions;

/// <summary>A chave de idempotência já foi usada para uma operação com conteúdo diferente.</summary>
public sealed class IdempotencyConflictException(string idempotencyKey)
    : Exception($"A chave de idempotência '{idempotencyKey}' já foi utilizada com dados diferentes.")
{
    public string IdempotencyKey { get; } = idempotencyKey;
}
