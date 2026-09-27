using Transfers.Domain.Exceptions;
using Transfers.Domain.Limits;
using Transfers.Domain.Transfers;

namespace Transfers.UnitTests.Domain;

public sealed class TransferStateTests
{
    [Fact]
    public void Cancel_ScheduledTransfer_BecomesCancelled()
    {
        var transfer = Scheduled();
        var cancelledAt = TestData.Now.AddMinutes(10);

        transfer.Cancel(cancelledAt);

        Assert.Equal(TransferStatus.Cancelled, transfer.Status);
        Assert.Equal(cancelledAt, transfer.CancelledAt);
    }

    [Fact]
    public void Cancel_CompletedTransfer_Throws()
    {
        var transfer = Transfer.CreateImmediate(1, 2, 100m, TestData.Now);
        transfer.Execute(TestData.ActiveAccount(1, 1_000m), TestData.ActiveAccount(2, 0m), HourlyUsage.None, TestData.DayLimit, TestData.Now);

        var exception = Assert.Throws<InvalidTransferStateException>(() => transfer.Cancel(TestData.Now));

        Assert.Equal(TransferStatus.Completed, exception.CurrentStatus);
    }

    [Fact]
    public void Cancel_FailedTransfer_Throws()
    {
        var transfer = Transfer.CreateImmediate(1, 2, 100m, TestData.Now);
        transfer.Execute(TestData.ActiveAccount(1, 0m), TestData.ActiveAccount(2, 0m), HourlyUsage.None, TestData.DayLimit, TestData.Now);

        Assert.Equal(TransferStatus.Failed, transfer.Status);
        Assert.Throws<InvalidTransferStateException>(() => transfer.Cancel(TestData.Now));
    }

    [Fact]
    public void Cancel_AlreadyCancelledTransfer_Throws()
    {
        var transfer = Scheduled();
        transfer.Cancel(TestData.Now);

        Assert.Throws<InvalidTransferStateException>(() => transfer.Cancel(TestData.Now));
    }

    [Fact]
    public void Cancel_ProcessingTransfer_Throws()
    {
        var transfer = Transfer.CreateImmediate(1, 2, 100m, TestData.Now);

        Assert.Throws<InvalidTransferStateException>(() => transfer.Cancel(TestData.Now));
    }

    [Fact]
    public void StartProcessing_WhenDue_MovesToProcessing()
    {
        var transfer = Scheduled();

        transfer.StartProcessing(transfer.ScheduledFor!.Value);

        Assert.Equal(TransferStatus.Processing, transfer.Status);
    }

    [Fact]
    public void StartProcessing_BeforeScheduledDate_Throws()
    {
        var transfer = Scheduled();

        Assert.Throws<DomainException>(() => transfer.StartProcessing(TestData.Now));
        Assert.Equal(TransferStatus.Scheduled, transfer.Status);
    }

    [Fact]
    public void StartProcessing_CancelledTransfer_Throws()
    {
        var transfer = Scheduled();
        transfer.Cancel(TestData.Now);

        Assert.Throws<InvalidTransferStateException>(() => transfer.StartProcessing(transfer.ScheduledFor!.Value));
    }

    [Fact]
    public void Execute_ScheduledTransferWithoutStartProcessing_Throws()
    {
        var transfer = Scheduled();

        Assert.Throws<InvalidTransferStateException>(() =>
            transfer.Execute(TestData.ActiveAccount(1, 1_000m), TestData.ActiveAccount(2, 0m), HourlyUsage.None, TestData.DayLimit, TestData.Now));
    }

    [Fact]
    public void Execute_Twice_Throws()
    {
        var transfer = Transfer.CreateImmediate(1, 2, 100m, TestData.Now);
        var source = TestData.ActiveAccount(1, 1_000m);
        var destination = TestData.ActiveAccount(2, 0m);
        transfer.Execute(source, destination, HourlyUsage.None, TestData.DayLimit, TestData.Now);

        Assert.Throws<InvalidTransferStateException>(() =>
            transfer.Execute(source, destination, HourlyUsage.None, TestData.DayLimit, TestData.Now));
        Assert.Equal(900m, source.Balance);
    }

    [Fact]
    public void Fail_ProcessingTransfer_BecomesFailedWithReason()
    {
        var transfer = Scheduled();
        transfer.StartProcessing(transfer.ScheduledFor!.Value);

        transfer.Fail(TransferRejectionReason.AccountNotFound, TestData.Now.AddHours(1));

        Assert.Equal(TransferStatus.Failed, transfer.Status);
        Assert.Equal(TransferRejectionReason.AccountNotFound, transfer.FailureReason);
    }

    private static Transfer Scheduled() =>
        Transfer.Schedule(1, 2, 100m, TestData.Now.AddHours(1), TestData.Now);
}
