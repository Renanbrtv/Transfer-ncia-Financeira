namespace Transfers.Domain.Exceptions;

/// <summary>
/// Violação de uma regra de negócio. O <see cref="Code"/> é estável e pode ser usado por clientes
/// da API; a mensagem é voltada para leitura humana.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
