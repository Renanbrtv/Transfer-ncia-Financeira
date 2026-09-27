using System.Net;
using Transfers.Application.Transfers;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;
using Transfers.IntegrationTests.Infrastructure;

namespace Transfers.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ScheduledTransfersTests(TransfersApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Schedule_WithFutureDate_Returns201WithoutMovingMoney()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);

        var transfer = await ScheduleSuccessfullyAsync(source, destination, 300m, TimeSpan.FromHours(1));

        Assert.Equal(TransferStatus.Scheduled, transfer.Status);
        Assert.Equal(TransferType.Scheduled, transfer.Type);
        Assert.Equal(1_000m, (await GetAccountAsync(source)).Balance);
    }

    [Fact]
    public async Task Schedule_WithPastDate_Returns400()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);

        var response = await ScheduleAsync(source, destination, 100m, Factory.Clock.GetUtcNow().AddMinutes(-1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("transfer.schedule_not_in_future", await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Schedule_ForUnknownAccount_Returns404()
    {
        var source = await CreateAccountAsync(balance: 1_000m);

        var response = await ScheduleAsync(source, 999_999, 100m, Factory.Clock.GetUtcNow().AddHours(1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ScheduledTransfer_IsNotExecutedBeforeItsTime()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        var transfer = await ScheduleSuccessfullyAsync(source, destination, 300m, TimeSpan.FromHours(1));

        Factory.Clock.Advance(TimeSpan.FromMinutes(59));
        await ProcessDueScheduledTransfersAsync();

        Assert.Equal(TransferStatus.Scheduled, (await GetTransferAsync(transfer.Id)).Status);
    }

    [Fact]
    public async Task ScheduledTransfer_WhenDue_IsCompleted()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        var transfer = await ScheduleSuccessfullyAsync(source, destination, 300m, TimeSpan.FromHours(1));

        Factory.Clock.Advance(TimeSpan.FromHours(1));
        await ProcessDueScheduledTransfersAsync();

        var processed = await GetTransferAsync(transfer.Id);
        Assert.Equal(TransferStatus.Completed, processed.Status);
        Assert.NotNull(processed.ProcessedAt);
        Assert.Equal(700m, (await GetAccountAsync(source)).Balance);
        Assert.Equal(300m, (await GetAccountAsync(destination)).Balance);
    }

    // Cenário 8
    [Fact]
    public async Task ScheduledTransfer_WithoutBalanceAtExecutionTime_Fails()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        var transfer = await ScheduleSuccessfullyAsync(source, destination, 900m, TimeSpan.FromHours(2));

        // O saldo existia no agendamento, mas é consumido antes da execução.
        Assert.Equal(HttpStatusCode.Created, (await TransferAsync(source, destination, 500m)).StatusCode);

        Factory.Clock.Advance(TimeSpan.FromHours(2));
        await ProcessDueScheduledTransfersAsync();

        var processed = await GetTransferAsync(transfer.Id);
        Assert.Equal(TransferStatus.Failed, processed.Status);
        Assert.Equal(nameof(TransferRejectionReason.InsufficientFunds), processed.FailureCode);
        Assert.Equal(500m, (await GetAccountAsync(source)).Balance);
    }

    [Fact]
    public async Task ScheduledTransfer_ToBlockedAccount_FailsOnlyAtExecution()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m, status: AccountStatus.Blocked);
        var transfer = await ScheduleSuccessfullyAsync(source, destination, 100m, TimeSpan.FromHours(1));

        Factory.Clock.Advance(TimeSpan.FromHours(1));
        await ProcessDueScheduledTransfersAsync();

        var processed = await GetTransferAsync(transfer.Id);
        Assert.Equal(TransferStatus.Failed, processed.Status);
        Assert.Equal(nameof(TransferRejectionReason.DestinationAccountNotActive), processed.FailureCode);
    }

    // Cenário 9
    [Fact]
    public async Task ScheduledTransfer_CancelledBeforeExecution_IsNeverExecuted()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        var transfer = await ScheduleSuccessfullyAsync(source, destination, 300m, TimeSpan.FromHours(1));

        var cancelResponse = await Client.PostAsync($"/api/transfers/{transfer.Id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await ReadAsync<TransferResponse>(cancelResponse);
        Assert.Equal(TransferStatus.Cancelled, cancelled!.Status);
        Assert.NotNull(cancelled.CancelledAt);

        Factory.Clock.Advance(TimeSpan.FromHours(2));
        await ProcessDueScheduledTransfersAsync();

        Assert.Equal(TransferStatus.Cancelled, (await GetTransferAsync(transfer.Id)).Status);
        Assert.Equal(1_000m, (await GetAccountAsync(source)).Balance);
    }

    [Fact]
    public async Task Cancel_CompletedTransfer_Returns409()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        var transfer = await ReadAsync<TransferResponse>(await TransferAsync(source, destination, 100m));

        var response = await Client.PostAsync($"/api/transfers/{transfer!.Id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("transfer.invalid_state", await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Cancel_TwiceTheSameTransfer_SecondReturns409()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        var transfer = await ScheduleSuccessfullyAsync(source, destination, 100m, TimeSpan.FromHours(1));

        var first = await Client.PostAsync($"/api/transfers/{transfer.Id}/cancel", content: null);
        var second = await Client.PostAsync($"/api/transfers/{transfer.Id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Cancel_UnknownTransfer_Returns404()
    {
        var response = await Client.PostAsync($"/api/transfers/{Guid.NewGuid()}/cancel", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
