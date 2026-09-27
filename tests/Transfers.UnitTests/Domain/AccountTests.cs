using Transfers.Domain.Accounts;
using Transfers.Domain.Exceptions;

namespace Transfers.UnitTests.Domain;

public sealed class AccountTests
{
    [Fact]
    public void AvailableBalance_IsBalancePlusOverdraft()
    {
        var account = TestData.ActiveAccount(1, balance: 500m, overdraftLimit: 1_000m);

        Assert.Equal(1_500m, account.AvailableBalance);
    }

    [Fact]
    public void Debit_WithSufficientBalance_DecreasesBalance()
    {
        var account = TestData.ActiveAccount(1, balance: 1_000m);

        account.Debit(400m);

        Assert.Equal(600m, account.Balance);
    }

    [Fact]
    public void Debit_UsingOverdraft_LeavesNegativeBalance()
    {
        var account = TestData.ActiveAccount(1, balance: 500m, overdraftLimit: 1_000m);

        account.Debit(1_000m);

        Assert.Equal(-500m, account.Balance);
    }

    [Fact]
    public void Debit_ExactlyTheAvailableBalance_IsAllowed()
    {
        var account = TestData.ActiveAccount(1, balance: 500m, overdraftLimit: 1_000m);

        account.Debit(1_500m);

        Assert.Equal(-1_000m, account.Balance);
        Assert.Equal(0m, account.AvailableBalance);
    }

    [Fact]
    public void Debit_AboveBalancePlusOverdraft_ThrowsAndKeepsBalance()
    {
        var account = TestData.ActiveAccount(1, balance: 500m, overdraftLimit: 1_000m);

        var exception = Assert.Throws<DomainException>(() => account.Debit(1_500.01m));

        Assert.Equal("account.insufficient_funds", exception.Code);
        Assert.Equal(500m, account.Balance);
    }

    [Fact]
    public void Credit_OnNegativeBalance_CoversOverdraftFirst()
    {
        var account = TestData.ActiveAccount(1, balance: 0m, overdraftLimit: 1_000m);
        account.Debit(800m);

        account.Credit(1_000m);

        Assert.Equal(200m, account.Balance);
    }

    [Theory]
    [InlineData(AccountStatus.Blocked)]
    [InlineData(AccountStatus.Inactive)]
    public void DebitAndCredit_OnNonActiveAccount_Throw(AccountStatus status)
    {
        var account = TestData.AccountWithStatus(1, status);

        Assert.Throws<DomainException>(() => account.Debit(10m));
        Assert.Throws<DomainException>(() => account.Credit(10m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Debit_WithNonPositiveAmount_ThrowsValidation(int amount)
    {
        var account = TestData.ActiveAccount(1, balance: 1_000m);

        Assert.Throws<DomainValidationException>(() => account.Debit(amount));
    }

    [Fact]
    public void Open_WithNegativeOverdraft_ThrowsValidation()
    {
        Assert.Throws<DomainValidationException>(() => Account.Open("Ana", 100m, -1m));
    }

    [Fact]
    public void Open_WithoutHolderName_ThrowsValidation()
    {
        Assert.Throws<DomainValidationException>(() => Account.Open("  ", 100m, 0m));
    }

    [Fact]
    public void Block_ChangesStatusAndPreventsOperations()
    {
        var account = Account.Open("Ana", 100m, 0m);

        account.Block();

        Assert.Equal(AccountStatus.Blocked, account.Status);
        Assert.False(account.IsActive);
    }
}
