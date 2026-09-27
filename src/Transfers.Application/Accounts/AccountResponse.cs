using Transfers.Domain.Accounts;

namespace Transfers.Application.Accounts;

/// <summary>Dados da conta.</summary>
/// <param name="Id">Identificador da conta.</param>
/// <param name="HolderName">Nome do titular.</param>
/// <param name="Balance">Saldo atual. Negativo quando o cheque especial está em uso.</param>
/// <param name="OverdraftLimit">Limite de cheque especial.</param>
/// <param name="AvailableBalance">Saldo + cheque especial: quanto a conta ainda pode transferir.</param>
/// <param name="Status">Active, Blocked ou Inactive.</param>
public sealed record AccountResponse(
    int Id,
    string HolderName,
    decimal Balance,
    decimal OverdraftLimit,
    decimal AvailableBalance,
    AccountStatus Status)
{
    public static AccountResponse From(Account account) =>
        new(
            account.Id,
            account.HolderName,
            account.Balance,
            account.OverdraftLimit,
            account.AvailableBalance,
            account.Status);
}
