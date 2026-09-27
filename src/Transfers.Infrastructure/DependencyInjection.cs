using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Transfers.Application.Abstractions;
using Transfers.Infrastructure.Persistence;
using Transfers.Infrastructure.Repositories;

namespace Transfers.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "TransfersDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // A connection string é lida na criação do DbContext (e não no registro), o que permite
        // que testes de integração a substituam depois que o host já foi configurado.
        services.AddDbContext<TransfersDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' não configurada. Defina ConnectionStrings:{ConnectionStringName} " +
                    $"(appsettings, user-secrets ou variável de ambiente ConnectionStrings__{ConnectionStringName}).");
            }

            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<TransfersDbContext>());
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<ITransferAttemptRepository, TransferAttemptRepository>();

        return services;
    }

    /// <summary>
    /// Aplica as migrations pendentes. Tenta algumas vezes porque, no docker-compose, o SQL Server
    /// pode ainda estar inicializando quando a API sobe.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(5);

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransfersDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TransfersDbContext>>();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Migrations aplicadas com sucesso.");
                return;
            }
            catch (SqlException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Banco de dados indisponível (tentativa {Attempt}/{MaxAttempts}). Nova tentativa em {Delay}s.",
                    attempt, maxAttempts, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
