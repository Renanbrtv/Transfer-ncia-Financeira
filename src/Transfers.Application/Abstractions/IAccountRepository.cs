using Transfers.Domain.Accounts;

namespace Transfers.Application.Abstractions;

public interface IAccountRepository
{
    /// <summary>Leitura sem rastreamento, para consultas.</summary>
    Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Carrega a conta com lock exclusivo de atualização (UPDLOCK), mantido até o fim da transação.
    /// Deve ser chamado dentro de uma transação aberta por <see cref="IUnitOfWork"/>.
    /// </summary>
    Task<Account?> GetForUpdateAsync(int id, CancellationToken cancellationToken);
}
