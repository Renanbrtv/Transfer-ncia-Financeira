using Transfers.Domain.Exceptions;

namespace Transfers.Domain.Accounts;

/// <summary>
/// Conta corrente com cheque especial. O saldo pode ficar negativo até <see cref="OverdraftLimit"/>;
/// um saldo negativo representa exatamente o valor já utilizado do limite.
/// </summary>
public sealed class Account
{
    public const int HolderNameMaxLength = 100;

    // Construtor usado pelo EF Core na materialização.
    private Account()
    {
        HolderName = string.Empty;
    }

    private Account(string holderName, decimal balance, decimal overdraftLimit, AccountStatus status)
    {
        HolderName = holderName;
        Balance = balance;
        OverdraftLimit = overdraftLimit;
        Status = status;
    }

    public int Id { get; private set; }

    public string HolderName { get; private set; }

    public decimal Balance { get; private set; }

    public decimal OverdraftLimit { get; private set; }

    public AccountStatus Status { get; private set; }

    /// <summary>Token de concorrência otimista (rowversion do SQL Server).</summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Quanto a conta ainda pode movimentar: saldo + cheque especial.</summary>
    public decimal AvailableBalance => Balance + OverdraftLimit;

    public bool IsActive => Status == AccountStatus.Active;

    public static Account Open(string holderName, decimal initialBalance, decimal overdraftLimit)
    {
        if (string.IsNullOrWhiteSpace(holderName))
        {
            throw new DomainValidationException("account.holder_required", "O nome do titular é obrigatório.");
        }

        if (holderName.Trim().Length > HolderNameMaxLength)
        {
            throw new DomainValidationException(
                "account.holder_too_long",
                $"O nome do titular deve ter no máximo {HolderNameMaxLength} caracteres.");
        }

        if (initialBalance < 0)
        {
            throw new DomainValidationException("account.negative_initial_balance", "O saldo inicial não pode ser negativo.");
        }

        if (overdraftLimit < 0)
        {
            throw new DomainValidationException("account.negative_overdraft", "O limite de cheque especial não pode ser negativo.");
        }

        return new Account(holderName.Trim(), initialBalance, overdraftLimit, AccountStatus.Active);
    }

    public bool CanCover(decimal amount) => amount <= AvailableBalance;

    public void Debit(decimal amount)
    {
        Money.EnsureValidAmount(amount);
        EnsureActive();

        if (!CanCover(amount))
        {
            throw new DomainException(
                "account.insufficient_funds",
                $"A conta {Id} não possui saldo + cheque especial suficiente para debitar {amount:N2}.");
        }

        Balance -= amount;
    }

    public void Credit(decimal amount)
    {
        Money.EnsureValidAmount(amount);
        EnsureActive();

        Balance += amount;
    }

    public void Block() => Status = AccountStatus.Blocked;

    public void Deactivate() => Status = AccountStatus.Inactive;

    public void Activate() => Status = AccountStatus.Active;

    /// <summary>Uso exclusivo de testes: permite montar contas com Id sem depender do banco.</summary>
    internal static Account Restore(int id, string holderName, decimal balance, decimal overdraftLimit, AccountStatus status) =>
        new(holderName, balance, overdraftLimit, status) { Id = id };

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new DomainException("account.not_active", $"A conta {Id} não está ativa (status: {Status}).");
        }
    }
}
