using Transfers.Domain.Accounts;
using Transfers.Domain.Limits;
using Transfers.Domain.Transfers;

namespace Transfers.UnitTests.Domain;

public sealed class TransferExecutionTests
{
    [Fact]
    public void Execute_WithSufficientBalance_CompletesAndMovesMoney()
    {
        var source = TestData.ActiveAccount(1, balance: 1_000m);
        var destination = TestData.ActiveAccount(2, balance: 200m);
        var transfer = Immediate(amount: 300m);

        var attempt = transfer.Execute(source, destination, HourlyUsage.None, TestData.DayLimit, TestData.Now);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(TestData.Now, transfer.ProcessedAt);
        Assert.Null(transfer.FailureReason);
        Assert.Equal(700m, source.Balance);
        Assert.Equal(500m, destination.Balance);
        Assert.True(attempt.Succeeded);
        Assert.Equal(source.Id, attempt.AccountId);
        Assert.Equal(transfer.Id, attempt.TransferId);
        Assert.Equal(300m, attempt.Amount);
    }

    [Fact]
    public void Execute_UsingOverdraft_Completes()
    {
        var source = TestData.ActiveAccount(1, balance: 500m, overdraftLimit: 1_000m);
        var destination = TestData.ActiveAccount(2, balance: 0m);
        var transfer = Immediate(amount: 1_000m);

        transfer.Execute(source, destination, HourlyUsage.None, TestData.DayLimit, TestData.Now);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(-500m, source.Balance);
        Assert.Equal(1_000m, destination.Balance);
    }

    [Fact]
    public void Execute_ToAccountWithNegativeBalance_CreditsNormally()
    {
        var source = TestData.ActiveAccount(1, balance: 2_000m);
        var destination = TestData.ActiveAccount(2, balance: 0m, overdraftLimit: 1_000m);
        destination.Debit(800m);
        var transfer = Immediate(amount: 1_000m);

        transfer.Execute(source, destination, HourlyUsage.None, TestData.DayLimit, TestData.Now);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(200m, destination.Balance);
    }

    [Fact]
    public void Execute_AboveBalancePlusOverdraft_FailsWithoutMovingMoney()
    {
        var source = TestData.ActiveAccount(1, balance: 500m, overdraftLimit: 1_000m);
        var destination = TestData.ActiveAccount(2, balance: 0m);
        var transfer = Immediate(amount: 1_600m);

        var attempt = transfer.Execute(source, destination, HourlyUsage.None, TestData.DayLimit, TestData.Now);

        AssertRejected(transfer, attempt, TransferRejectionReason.InsufficientFunds);
        Assert.Equal(500m, source.Balance);
        Assert.Equal(0m, destination.Balance);
    }

    [Theory]
    [InlineData(AccountStatus.Blocked)]
    [InlineData(AccountStatus.Inactive)]
    public void Execute_FromNonActiveAccount_Fails(AccountStatus status)
    {
        var source = TestData.AccountWithStatus(1, status, balance: 5_000m);
        var destination = TestData.ActiveAccount(2, balance: 0m);
        var transfer = Immediate(amount: 100m);

        var attempt = transfer.Execute(source, destination, HourlyUsage.None, TestData.DayLimit, TestData.Now);

        AssertRejected(transfer, attempt, TransferRejectionReason.SourceAccountNotActive);
        Assert.Equal(5_000m, source.Balance);
    }

    [Theory]
    [InlineData(AccountStatus.Blocked)]
    [InlineData(AccountStatus.Inactive)]
    public void Execute_ToNonActiveAccount_Fails(AccountStatus status)
    {
        var source = TestData.ActiveAccount(1, balance: 5_000m);
        var destination = TestData.AccountWithStatus(2, status, balance: 0m);
        var transfer = Immediate(amount: 100m);

        var attempt = transfer.Execute(source, destination, HourlyUsage.None, TestData.DayLimit, TestData.Now);

        AssertRejected(transfer, attempt, TransferRejectionReason.DestinationAccountNotActive);
        Assert.Equal(5_000m, source.Balance);
        Assert.Equal(0m, destination.Balance);
    }

    [Fact]
    public void Execute_WhenHourlyAmountWouldBeExceeded_Fails()
    {
        var source = TestData.ActiveAccount(1, balance: 10_000m);
        var destination = TestData.ActiveAccount(2, balance: 0m);
        var usage = new HourlyUsage(Attempts: 1, TransferredAmount: 4_500m);
        var transfer = Immediate(amount: 600m);

        var attempt = transfer.Execute(source, destination, usage, TestData.DayLimit, TestData.Now);

        AssertRejected(transfer, attempt, TransferRejectionReason.HourlyAmountLimitExceeded);
        Assert.Equal(10_000m, source.Balance);
    }

    [Fact]
    public void Execute_ReachingExactlyTheHourlyAmount_Completes()
    {
        var source = TestData.ActiveAccount(1, balance: 10_000m);
        var destination = TestData.ActiveAccount(2, balance: 0m);
        var usage = new HourlyUsage(Attempts: 1, TransferredAmount: 4_500m);
        var transfer = Immediate(amount: 500m);

        transfer.Execute(source, destination, usage, TestData.DayLimit, TestData.Now);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
    }

    [Fact]
    public void Execute_WhenAttemptLimitReached_Fails()
    {
        var source = TestData.ActiveAccount(1, balance: 10_000m);
        var destination = TestData.ActiveAccount(2, balance: 0m);
        var usage = new HourlyUsage(Attempts: TestData.DayLimit.MaxAttemptsPerHour, TransferredAmount: 0m);
        var transfer = Immediate(amount: 10m);

        var attempt = transfer.Execute(source, destination, usage, TestData.DayLimit, TestData.Now);

        AssertRejected(transfer, attempt, TransferRejectionReason.HourlyAttemptLimitExceeded);
    }

    [Fact]
    public void Execute_LastAllowedAttempt_Completes()
    {
        var source = TestData.ActiveAccount(1, balance: 10_000m);
        var destination = TestData.ActiveAccount(2, balance: 0m);
        var usage = new HourlyUsage(Attempts: TestData.DayLimit.MaxAttemptsPerHour - 1, TransferredAmount: 0m);
        var transfer = Immediate(amount: 10m);

        transfer.Execute(source, destination, usage, TestData.DayLimit, TestData.Now);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
    }

    [Fact]
    public void Execute_WithNightLimit_RejectsAmountAllowedDuringTheDay()
    {
        var transfer = Immediate(amount: 1_500m);

        var attempt = transfer.Execute(
            TestData.ActiveAccount(1, balance: 10_000m),
            TestData.ActiveAccount(2, balance: 0m),
            HourlyUsage.None,
            TestData.NightLimit,
            TestData.Now);

        AssertRejected(transfer, attempt, TransferRejectionReason.HourlyAmountLimitExceeded);
    }

    [Fact]
    public void Execute_ScheduledTransferAfterStartProcessing_RechecksBalance()
    {
        var source = TestData.ActiveAccount(1, balance: 100m);
        var destination = TestData.ActiveAccount(2, balance: 0m);
        var transfer = Transfer.Schedule(1, 2, 500m, TestData.Now.AddHours(1), TestData.Now);
        var executionTime = TestData.Now.AddHours(1);
        transfer.StartProcessing(executionTime);

        var attempt = transfer.Execute(source, destination, HourlyUsage.None, TestData.DayLimit, executionTime);

        AssertRejected(transfer, attempt, TransferRejectionReason.InsufficientFunds);
        Assert.Equal(executionTime, transfer.ProcessedAt);
    }

    [Fact]
    public void Execute_WithAccountsThatDoNotMatchTheTransfer_Throws()
    {
        var transfer = Immediate(amount: 10m);

        Assert.Throws<InvalidOperationException>(() => transfer.Execute(
            TestData.ActiveAccount(7, balance: 1_000m),
            TestData.ActiveAccount(2, balance: 0m),
            HourlyUsage.None,
            TestData.DayLimit,
            TestData.Now));
    }

    private static Transfer Immediate(decimal amount) => Transfer.CreateImmediate(1, 2, amount, TestData.Now);

    private static void AssertRejected(Transfer transfer, TransferAttempt attempt, TransferRejectionReason expectedReason)
    {
        Assert.Equal(TransferStatus.Failed, transfer.Status);
        Assert.Equal(expectedReason, transfer.FailureReason);
        Assert.False(attempt.Succeeded);
        Assert.Equal(expectedReason, attempt.RejectionReason);
    }
}
