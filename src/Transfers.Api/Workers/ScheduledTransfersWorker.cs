using Microsoft.Extensions.Options;
using Transfers.Application.Options;
using Transfers.Application.Transfers;

namespace Transfers.Api.Workers;

/// <summary>
/// Verifica periodicamente as transferências agendadas vencidas e as executa.
/// Cada transferência é processada em um escopo de DI próprio (DbContext novo), para que a falha
/// de uma não contamine as demais.
/// </summary>
internal sealed class ScheduledTransfersWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ScheduledTransfersOptions> options,
    ILogger<ScheduledTransfersWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Processamento de transferências agendadas desabilitado por configuração.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, settings.PollingIntervalSeconds));
        logger.LogInformation("Worker de transferências agendadas iniciado (intervalo: {Interval}).", interval);

        using var timer = new PeriodicTimer(interval);
        try
        {
            do
            {
                await ProcessDueTransfersAsync(settings.BatchSize, stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker de transferências agendadas finalizado.");
        }
    }

    private async Task ProcessDueTransfersAsync(int batchSize, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> dueIds;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<ScheduledTransferProcessor>();
            dueIds = await processor.GetDueTransferIdsAsync(Math.Max(1, batchSize), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao buscar transferências agendadas vencidas.");
            return;
        }

        foreach (var transferId in dueIds)
        {
            await ProcessOneAsync(transferId, cancellationToken);
        }
    }

    private async Task ProcessOneAsync(Guid transferId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<ScheduledTransferProcessor>();
            await processor.ProcessAsync(transferId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A transação foi desfeita: a transferência continua Scheduled e será tentada no próximo ciclo.
            logger.LogError(ex, "Falha ao processar a transferência agendada {TransferId}.", transferId);
        }
    }
}
