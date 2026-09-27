using Transfers.Domain.Accounts;

namespace Transfers.Infrastructure.Persistence.Configurations;

/// <summary>Contas iniciais para facilitar a avaliação (aplicadas pela migration inicial).</summary>
internal static class AccountSeed
{
    public static readonly object[] Accounts =
    [
        new { Id = 1, HolderName = "João", Balance = 5000m, OverdraftLimit = 1000m, Status = AccountStatus.Active },
        new { Id = 2, HolderName = "Maria", Balance = 2000m, OverdraftLimit = 500m, Status = AccountStatus.Active },
        new { Id = 3, HolderName = "Carlos", Balance = 500m, OverdraftLimit = 1000m, Status = AccountStatus.Active },
        new { Id = 4, HolderName = "Conta Bloqueada", Balance = 5000m, OverdraftLimit = 1000m, Status = AccountStatus.Blocked },
    ];
}
