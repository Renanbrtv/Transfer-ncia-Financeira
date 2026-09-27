using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Domain.Accounts;
using Transfers.Infrastructure.Persistence;

namespace Transfers.Infrastructure.Repositories;

internal sealed class AccountRepository(TransfersDbContext dbContext) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Accounts.AsNoTracking().SingleOrDefaultAsync(account => account.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Accounts.AsNoTracking().OrderBy(account => account.Id).ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Accounts.AnyAsync(account => account.Id == id, cancellationToken);

    public Task<Account?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        // UPDLOCK: lock de atualização mantido até o fim da transação. Outra transação que tente travar
        // a mesma conta espera; leituras comuns não são bloqueadas. ROWLOCK evita escalar para página.
        // FromSql parametriza a interpolação ({id} vira @p0), então não há risco de SQL injection.
        dbContext.Accounts
            .FromSql($"SELECT * FROM [Accounts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
            .SingleOrDefaultAsync(cancellationToken);
}
