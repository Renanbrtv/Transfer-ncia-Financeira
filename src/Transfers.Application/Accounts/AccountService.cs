using Transfers.Application.Abstractions;
using Transfers.Application.Exceptions;
using Transfers.Application.Transfers;

namespace Transfers.Application.Accounts;

public sealed class AccountService(IAccountRepository accounts, ITransferRepository transfers)
{
    public const int MaxTransfersPerQuery = 100;

    public async Task<AccountResponse> GetByIdAsync(int accountId, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException("Conta", accountId);

        return AccountResponse.From(account);
    }

    public async Task<IReadOnlyList<AccountResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var result = await accounts.ListAsync(cancellationToken);
        return result.Select(AccountResponse.From).ToList();
    }

    /// <summary>Transferências enviadas ou recebidas pela conta, das mais recentes para as mais antigas.</summary>
    public async Task<IReadOnlyList<TransferResponse>> ListTransfersAsync(int accountId, int take, CancellationToken cancellationToken)
    {
        if (!await accounts.ExistsAsync(accountId, cancellationToken))
        {
            throw new NotFoundException("Conta", accountId);
        }

        var result = await transfers.ListByAccountAsync(accountId, Math.Clamp(take, 1, MaxTransfersPerQuery), cancellationToken);
        return result.Select(TransferResponse.From).ToList();
    }
}
