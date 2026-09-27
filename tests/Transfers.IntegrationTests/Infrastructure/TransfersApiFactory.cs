using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.MsSql;

namespace Transfers.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe a API em memória apontando para um SQL Server real em container.
/// SQLite ou InMemory não serviriam: não suportam UPDLOCK nem o comportamento de lock do SQL Server,
/// e o teste de concorrência deixaria de provar alguma coisa.
/// </summary>
public sealed class TransfersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private readonly MsSqlContainer _database = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    /// <summary>
    /// Relógio controlado pelos testes. Começa em 15/01/2026 10:00 em São Paulo (período diurno)
    /// e só anda para frente.
    /// </summary>
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 1, 15, 13, 0, 0, TimeSpan.Zero));

    public Task InitializeAsync() => _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    /// <summary>Avança o relógio até a próxima ocorrência do horário local informado.</summary>
    public void AdvanceToLocalTime(TimeOnly localTime)
    {
        var nowLocal = TimeZoneInfo.ConvertTime(Clock.GetUtcNow(), SaoPaulo);
        var target = new DateTimeOffset(nowLocal.Date.Add(localTime.ToTimeSpan()), nowLocal.Offset);
        if (target <= nowLocal)
        {
            target = target.AddDays(1);
        }

        Clock.Advance(target - nowLocal);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:TransfersDb", _database.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");

        // O worker fica desligado; os testes disparam o processamento manualmente para serem determinísticos.
        builder.UseSetting("ScheduledTransfers:Enabled", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<TransfersApiFactory>
{
    public const string Name = "Transfers API";
}
