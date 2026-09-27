namespace Transfers.Application.Options;

/// <summary>Seção "ScheduledTransfers" do appsettings: controla o worker de agendamentos.</summary>
public sealed class ScheduledTransfersOptions
{
    public const string SectionName = "ScheduledTransfers";

    public bool Enabled { get; set; } = true;

    public int PollingIntervalSeconds { get; set; } = 10;

    public int BatchSize { get; set; } = 50;
}
