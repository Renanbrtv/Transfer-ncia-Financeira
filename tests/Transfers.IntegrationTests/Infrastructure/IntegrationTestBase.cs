using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Transfers.Application.Accounts;
using Transfers.Application.Transfers;
using Transfers.Domain.Accounts;
using Transfers.Infrastructure.Persistence;

namespace Transfers.IntegrationTests.Infrastructure;

/// <summary>
/// Base dos testes de integração. Cada teste cria as próprias contas, então os testes não dependem
/// uns dos outros nem dos dados iniciais. As classes concretas precisam do atributo
/// [Collection(ApiCollection.Name)] (o xUnit não herda o atributo da classe base).
/// </summary>
public abstract class IntegrationTestBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected IntegrationTestBase(TransfersApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();

        // Garante que cada teste começa fora da janela de uma hora dos testes anteriores.
        factory.Clock.Advance(TimeSpan.FromHours(2));
        factory.AdvanceToLocalTime(new TimeOnly(10, 0));
    }

    protected TransfersApiFactory Factory { get; }

    protected HttpClient Client { get; }

    protected async Task<int> CreateAccountAsync(decimal balance, decimal overdraftLimit = 0m, AccountStatus status = AccountStatus.Active)
    {
        var account = Account.Open("Conta de teste", balance, overdraftLimit);
        if (status == AccountStatus.Blocked)
        {
            account.Block();
        }
        else if (status == AccountStatus.Inactive)
        {
            account.Deactivate();
        }

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransfersDbContext>();
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        return account.Id;
    }

    protected async Task<AccountResponse> GetAccountAsync(int accountId)
    {
        var account = await Client.GetFromJsonAsync<AccountResponse>($"/api/accounts/{accountId}", JsonOptions);
        return account!;
    }

    protected Task<HttpResponseMessage> TransferAsync(int sourceAccountId, int destinationAccountId, decimal amount, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
        {
            Content = JsonContent.Create(new { sourceAccountId, destinationAccountId, amount })
        };

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return Client.SendAsync(request);
    }

    protected Task<HttpResponseMessage> ScheduleAsync(int sourceAccountId, int destinationAccountId, decimal amount, DateTimeOffset scheduledFor) =>
        Client.PostAsJsonAsync("/api/transfers/scheduled", new { sourceAccountId, destinationAccountId, amount, scheduledFor });

    protected async Task<TransferResponse> ScheduleSuccessfullyAsync(int sourceAccountId, int destinationAccountId, decimal amount, TimeSpan delay)
    {
        var response = await ScheduleAsync(sourceAccountId, destinationAccountId, amount, Factory.Clock.GetUtcNow().Add(delay));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync<TransferResponse>(response))!;
    }

    protected async Task<TransferResponse> GetTransferAsync(Guid transferId)
    {
        var transfer = await Client.GetFromJsonAsync<TransferResponse>($"/api/transfers/{transferId}", JsonOptions);
        return transfer!;
    }

    /// <summary>Executa o mesmo processamento do worker, de forma síncrona e determinística.</summary>
    protected async Task ProcessDueScheduledTransfersAsync()
    {
        IReadOnlyList<Guid> dueIds;
        using (var scope = Factory.Services.CreateScope())
        {
            dueIds = await scope.ServiceProvider.GetRequiredService<ScheduledTransferProcessor>()
                .GetDueTransferIdsAsync(100, CancellationToken.None);
        }

        foreach (var transferId in dueIds)
        {
            using var scope = Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ScheduledTransferProcessor>()
                .ProcessAsync(transferId, CancellationToken.None);
        }
    }

    protected static Task<T?> ReadAsync<T>(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<T>(JsonOptions);

    /// <summary>Lê o campo "code" do ProblemDetails.</summary>
    protected static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
