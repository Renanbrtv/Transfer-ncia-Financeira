namespace Transfers.Domain.Exceptions;

/// <summary>
/// Dados de entrada que nunca poderiam formar uma operação válida (valor zero, origem igual ao destino, data passada...).
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string code, string message)
        : base(code, message)
    {
    }
}
