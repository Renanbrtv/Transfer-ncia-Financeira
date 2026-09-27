using Transfers.Domain.Exceptions;
using Transfers.Domain.Transfers;

namespace Transfers.UnitTests.Domain;

public sealed class TransferCreationTests
{
    [Fact]
    public void CreateImmediate_WithValidData_StartsInProcessing()
    {
        var transfer = Transfer.CreateImmediate(1, 2, 100m, TestData.Now);

        Assert.NotEqual(Guid.Empty, transfer.Id);
        Assert.Equal(TransferType.Immediate, transfer.Type);
        Assert.Equal(TransferStatus.Processing, transfer.Status);
        Assert.Equal(TestData.Now, transfer.CreatedAt);
        Assert.Null(transfer.ScheduledFor);
    }

    [Fact]
    public void CreateImmediate_WithSameSourceAndDestination_Throws()
    {
        var exception = Assert.Throws<DomainValidationException>(() => Transfer.CreateImmediate(1, 1, 100m, TestData.Now));

        Assert.Equal("transfer.same_account", exception.Code);
    }

    [Fact]
    public void CreateImmediate_WithZeroAmount_Throws()
    {
        var exception = Assert.Throws<DomainValidationException>(() => Transfer.CreateImmediate(1, 2, 0m, TestData.Now));

        Assert.Equal("amount.not_positive", exception.Code);
    }

    [Fact]
    public void CreateImmediate_WithNegativeAmount_Throws()
    {
        var exception = Assert.Throws<DomainValidationException>(() => Transfer.CreateImmediate(1, 2, -50m, TestData.Now));

        Assert.Equal("amount.not_positive", exception.Code);
    }

    [Fact]
    public void CreateImmediate_WithMoreThanTwoDecimalPlaces_Throws()
    {
        var exception = Assert.Throws<DomainValidationException>(() => Transfer.CreateImmediate(1, 2, 10.001m, TestData.Now));

        Assert.Equal("amount.invalid_scale", exception.Code);
    }

    [Fact]
    public void CreateImmediate_WithTooLongIdempotencyKey_Throws()
    {
        var key = new string('k', Transfer.IdempotencyKeyMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => Transfer.CreateImmediate(1, 2, 10m, TestData.Now, key));
    }

    [Fact]
    public void Schedule_WithFutureDate_StartsAsScheduled()
    {
        var scheduledFor = TestData.Now.AddHours(2);

        var transfer = Transfer.Schedule(1, 2, 100m, scheduledFor, TestData.Now);

        Assert.Equal(TransferType.Scheduled, transfer.Type);
        Assert.Equal(TransferStatus.Scheduled, transfer.Status);
        Assert.Equal(scheduledFor, transfer.ScheduledFor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-60)]
    public void Schedule_WithDateNotInTheFuture_Throws(int minutesFromNow)
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Transfer.Schedule(1, 2, 100m, TestData.Now.AddMinutes(minutesFromNow), TestData.Now));

        Assert.Equal("transfer.schedule_not_in_future", exception.Code);
    }

    [Fact]
    public void Schedule_WithSameSourceAndDestination_Throws()
    {
        Assert.Throws<DomainValidationException>(() => Transfer.Schedule(3, 3, 100m, TestData.Now.AddHours(1), TestData.Now));
    }

    [Fact]
    public void HasSameContentAs_ComparesAccountsAmountTypeAndDate()
    {
        var original = Transfer.CreateImmediate(1, 2, 100m, TestData.Now, "key-1");
        var same = Transfer.CreateImmediate(1, 2, 100m, TestData.Now.AddSeconds(5), "key-1");
        var differentAmount = Transfer.CreateImmediate(1, 2, 101m, TestData.Now, "key-1");

        Assert.True(original.HasSameContentAs(same));
        Assert.False(original.HasSameContentAs(differentAmount));
    }
}
