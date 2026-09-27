using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Transfers.Application.Accounts;
using Transfers.Application.Options;
using Transfers.Application.Transfers;
using Transfers.Domain.Limits;

namespace Transfers.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os serviços de aplicação. Espera que <see cref="TransferLimitsOptions"/> tenha sido
    /// configurado pelo host (seção "TransferLimits").
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<TransferLimitPolicy>(sp =>
            sp.GetRequiredService<IOptions<TransferLimitsOptions>>().Value.ToPolicy());

        services.AddScoped<TransferExecutor>();
        services.AddScoped<TransferService>();
        services.AddScoped<ScheduledTransferProcessor>();
        services.AddScoped<AccountService>();

        return services;
    }
}
