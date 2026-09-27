using System.Net;
using Transfers.Application.Transfers;
using Transfers.Domain.Transfers;
using Transfers.IntegrationTests.Infrastructure;

namespace Transfers.IntegrationTests;

/// <summary>
/// Requisições realmente simultâneas contra o SQL Server. Sem o lock pessimista (UPDLOCK) na conta
/// de origem, estes testes falhariam de forma intermitente.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ConcurrencyTests(TransfersApiFactory factory) : IntegrationTestBase(factory)
{
    // Cenário 15
    [Fact]
    public async Task TwoSimultaneousTransfers_UsingTheSameBalance_OnlyOneCompletes()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destinationA = await CreateAccountAsync(balance: 0m);
        var destinationB = await CreateAccountAsync(balance: 0m);

        var responses = await Task.WhenAll(
            TransferAsync(source, destinationA, 800m),
            TransferAsync(source, destinationB, 800m));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        var rejected = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Equal(nameof(TransferRejectionReason.InsufficientFunds), await ReadProblemCodeAsync(rejected));

        Assert.Equal(200m, (await GetAccountAsync(source)).Balance);
        var credited = (await GetAccountAsync(destinationA)).Balance + (await GetAccountAsync(destinationB)).Balance;
        Assert.Equal(800m, credited);
    }

    [Fact]
    public async Task ManySimultaneousTransfers_NeverOverdrawTheAccount()
    {
        var source = await CreateAccountAsync(balance: 1_000m, overdraftLimit: 500m);
        var destination = await CreateAccountAsync(balance: 0m);

        // 10 x 400 = 4.000 pedidos, mas só 1.500 disponíveis => no máximo 3 concluem.
        // O limite diurno de 5 tentativas/hora também é respeitado sob concorrência.
        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => TransferAsync(source, destination, 400m)));

        var completed = responses.Count(response => response.StatusCode == HttpStatusCode.Created);
        var account = await GetAccountAsync(source);

        Assert.Equal(3, completed);
        Assert.Equal(1_000m - (completed * 400m), account.Balance);
        Assert.True(account.Balance >= -account.OverdraftLimit);
        Assert.Equal(completed * 400m, (await GetAccountAsync(destination)).Balance);
    }

    [Fact]
    public async Task SimultaneousTransfers_RespectTheHourlyAmountLimit()
    {
        var source = await CreateAccountAsync(balance: 50_000m);
        var destination = await CreateAccountAsync(balance: 0m);

        // Limite diurno: R$ 5.000/hora. Três transferências de R$ 2.000 simultâneas => só duas cabem.
        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => TransferAsync(source, destination, 2_000m)));

        Assert.Equal(2, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        var rejected = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Equal(nameof(TransferRejectionReason.HourlyAmountLimitExceeded), await ReadProblemCodeAsync(rejected));
        Assert.Equal(46_000m, (await GetAccountAsync(source)).Balance);
    }

    [Fact]
    public async Task CrossedTransfers_BetweenTheSameAccounts_DoNotDeadlock()
    {
        var accountA = await CreateAccountAsync(balance: 1_000m);
        var accountB = await CreateAccountAsync(balance: 1_000m);

        // A→B e B→A ao mesmo tempo. Com lock em ordem crescente de Id não há deadlock.
        var responses = await Task.WhenAll(
            TransferAsync(accountA, accountB, 100m),
            TransferAsync(accountB, accountA, 100m),
            TransferAsync(accountA, accountB, 100m),
            TransferAsync(accountB, accountA, 100m));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        Assert.Equal(1_000m, (await GetAccountAsync(accountA)).Balance);
        Assert.Equal(1_000m, (await GetAccountAsync(accountB)).Balance);
    }

    [Fact]
    public async Task SimultaneousRequests_WithTheSameIdempotencyKey_CreateASingleTransfer()
    {
        var source = await CreateAccountAsync(balance: 1_000m);
        var destination = await CreateAccountAsync(balance: 0m);
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => TransferAsync(source, destination, 100m, key)));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Equal(2, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        var ids = new HashSet<Guid>();
        foreach (var response in responses)
        {
            ids.Add((await ReadAsync<TransferResponse>(response))!.Id);
        }

        Assert.Single(ids);
        Assert.Equal(900m, (await GetAccountAsync(source)).Balance);
    }
}
