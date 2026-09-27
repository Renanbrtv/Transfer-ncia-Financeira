using System.Net;
using System.Net.Http.Json;
using Transfers.Application.Accounts;
using Transfers.Application.Transfers;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;
using Transfers.IntegrationTests.Infrastructure;

namespace Transfers.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class TransfersEndpointTests(TransfersApiFactory factory) : IntegrationTestBase(factory)
{
    // Cenário 1
    [Fact]
    public async Task Transfer_WithSufficientBalance_Returns201AndMovesMoney()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 100m);

        var response = await TransferAsync(source, destination, 300m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var transfer = await ReadAsync<TransferResponse>(response);
        Assert.Equal(TransferStatus.Completed, transfer!.Status);
        Assert.Equal($"/api/transfers/{transfer.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal(700m, (await GetAccountAsync(source)).Balance);
        Assert.Equal(400m, (await GetAccountAsync(destination)).Balance);
    }

    // Cenário 2
    [Fact]
    public async Task Transfer_UsingOverdraft_LeavesNegativeBalance()
    {
        var source = await CreateAccountAsync(balance: 500m, overdraftLimit: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);

        var response = await TransferAsync(source, destination, 1_000m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var account = await GetAccountAsync(source);
        Assert.Equal(-500m, account.Balance);
        Assert.Equal(500m, account.AvailableBalance);
    }

    // Cenário 3
    [Fact]
    public async Task Transfer_AboveBalancePlusOverdraft_Returns422AndRecordsFailure()
    {
        var source = await CreateAccountAsync(balance: 500m, overdraftLimit: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);

        var response = await TransferAsync(source, destination, 1_500.01m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(nameof(TransferRejectionReason.InsufficientFunds), await ReadProblemCodeAsync(response));
        Assert.Equal(500m, (await GetAccountAsync(source)).Balance);
        Assert.Equal(0m, (await GetAccountAsync(destination)).Balance);

        var history = await Client.GetFromJsonAsync<List<TransferResponse>>($"/api/accounts/{source}/transfers", JsonOptions);
        var failed = Assert.Single(history!);
        Assert.Equal(TransferStatus.Failed, failed.Status);
        Assert.Equal(nameof(TransferRejectionReason.InsufficientFunds), failed.FailureCode);
    }

    // Cenário 4
    [Fact]
    public async Task Transfer_ToAccountWithNegativeBalance_CoversTheOverdraft()
    {
        var negative = await CreateAccountAsync(balance: 0m, overdraftLimit: 1_000m);
        var other = await CreateAccountAsync(balance: 5_000m);
        Assert.Equal(HttpStatusCode.Created, (await TransferAsync(negative, other, 800m)).StatusCode);

        var response = await TransferAsync(other, negative, 1_000m);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(200m, (await GetAccountAsync(negative)).Balance);
    }

    // Cenário 5
    [Fact]
    public async Task Transfer_AboveHourlyAmountLimit_Returns422()
    {
        var source = await CreateAccountAsync(balance: 20_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        Assert.Equal(HttpStatusCode.Created, (await TransferAsync(source, destination, 3_000m)).StatusCode);

        var response = await TransferAsync(source, destination, 2_500m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(nameof(TransferRejectionReason.HourlyAmountLimitExceeded), await ReadProblemCodeAsync(response));
        Assert.Equal(17_000m, (await GetAccountAsync(source)).Balance);
    }

    [Fact]
    public async Task Transfer_AfterTheOneHourWindow_IsAllowedAgain()
    {
        var source = await CreateAccountAsync(balance: 20_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        Assert.Equal(HttpStatusCode.Created, (await TransferAsync(source, destination, 5_000m)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await TransferAsync(source, destination, 1m)).StatusCode);

        Factory.Clock.Advance(TimeSpan.FromMinutes(61));

        Assert.Equal(HttpStatusCode.Created, (await TransferAsync(source, destination, 1_000m)).StatusCode);
    }

    // Cenário 6
    [Fact]
    public async Task Transfer_AfterTooManyAttempts_Returns422EvenForValidTransfer()
    {
        var source = await CreateAccountAsync(balance: 100m);
        var destination = await CreateAccountAsync(balance: 0m);

        // 5 tentativas rejeitadas por saldo (limite diurno: 5 tentativas/hora).
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await TransferAsync(source, destination, 1_000m)).StatusCode);
        }

        var response = await TransferAsync(source, destination, 10m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(nameof(TransferRejectionReason.HourlyAttemptLimitExceeded), await ReadProblemCodeAsync(response));
        Assert.Equal(100m, (await GetAccountAsync(source)).Balance);
    }

    // Cenário 7
    [Fact]
    public async Task Transfer_DuringTheNight_UsesNightLimits()
    {
        var source = await CreateAccountAsync(balance: 10_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        Factory.AdvanceToLocalTime(new TimeOnly(23, 0));

        var aboveNightLimit = await TransferAsync(source, destination, 1_200m);
        var withinNightLimit = await TransferAsync(source, destination, 800m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, aboveNightLimit.StatusCode);
        Assert.Equal(nameof(TransferRejectionReason.HourlyAmountLimitExceeded), await ReadProblemCodeAsync(aboveNightLimit));
        Assert.Equal(HttpStatusCode.Created, withinNightLimit.StatusCode);
        Assert.Equal(9_200m, (await GetAccountAsync(source)).Balance);
    }

    // Cenário 10
    [Fact]
    public async Task Transfer_ToBlockedAccount_Returns422()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var blocked = await CreateAccountAsync(balance: 0m, status: AccountStatus.Blocked);

        var response = await TransferAsync(source, blocked, 100m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(nameof(TransferRejectionReason.DestinationAccountNotActive), await ReadProblemCodeAsync(response));
        Assert.Equal(1_000m, (await GetAccountAsync(source)).Balance);
    }

    // Cenário 11
    [Fact]
    public async Task Transfer_FromBlockedAccount_Returns422()
    {
        var blocked = await CreateAccountAsync(balance: 1_000m, status: AccountStatus.Blocked);
        var destination = await CreateAccountAsync(balance: 0m);

        var response = await TransferAsync(blocked, destination, 100m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(nameof(TransferRejectionReason.SourceAccountNotActive), await ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Transfer_FromInactiveAccount_Returns422()
    {
        var inactive = await CreateAccountAsync(balance: 1_000m, status: AccountStatus.Inactive);
        var destination = await CreateAccountAsync(balance: 0m);

        var response = await TransferAsync(inactive, destination, 100m);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(nameof(TransferRejectionReason.SourceAccountNotActive), await ReadProblemCodeAsync(response));
    }

    // Cenário 12
    [Fact]
    public async Task Transfer_ToTheSameAccount_Returns400()
    {
        var account = await CreateAccountAsync(balance: 1_000m);

        var response = await TransferAsync(account, account, 100m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("transfer.same_account", await ReadProblemCodeAsync(response));
    }

    // Cenários 13 e 14
    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Transfer_WithNonPositiveAmount_Returns400(int amount)
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);

        var response = await TransferAsync(source, destination, amount);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("amount.not_positive", await ReadProblemCodeAsync(response));
        Assert.Equal(1_000m, (await GetAccountAsync(source)).Balance);
    }

    [Fact]
    public async Task Transfer_WithMissingFields_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/transfers", new { sourceAccountId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_ToUnknownAccount_Returns404()
    {
        var source = await CreateAccountAsync(balance: 1_000m);

        var response = await TransferAsync(source, 999_999, 100m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_RepeatedWithSameIdempotencyKey_ExecutesOnlyOnce()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        var key = Guid.NewGuid().ToString();

        var first = await TransferAsync(source, destination, 100m, key);
        var second = await TransferAsync(source, destination, 100m, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.True(second.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal((await ReadAsync<TransferResponse>(first))!.Id, (await ReadAsync<TransferResponse>(second))!.Id);
        Assert.Equal(900m, (await GetAccountAsync(source)).Balance);
    }

    [Fact]
    public async Task Transfer_ReusingIdempotencyKeyWithDifferentData_Returns409()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        var key = Guid.NewGuid().ToString();
        await TransferAsync(source, destination, 100m, key);

        var response = await TransferAsync(source, destination, 200m, key);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(900m, (await GetAccountAsync(source)).Balance);
    }

    [Fact]
    public async Task GetTransfer_Unknown_Returns404()
    {
        var response = await Client.GetAsync($"/api/transfers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAccount_SeededAccount_ReturnsAvailableBalance()
    {
        var response = await Client.GetAsync("/api/accounts/4");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var account = await ReadAsync<AccountResponse>(response);
        Assert.Equal("Conta Bloqueada", account!.HolderName);
        Assert.Equal(AccountStatus.Blocked, account.Status);
        Assert.Equal(account.Balance + account.OverdraftLimit, account.AvailableBalance);
    }

    [Fact]
    public async Task GetAccount_Unknown_Returns404()
    {
        var response = await Client.GetAsync("/api/accounts/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
