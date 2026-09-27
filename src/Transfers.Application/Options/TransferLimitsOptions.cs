using Transfers.Domain.Limits;

namespace Transfers.Application.Options;

/// <summary>Seção "TransferLimits" do appsettings.</summary>
public sealed class TransferLimitsOptions
{
    public const string SectionName = "TransferLimits";

    /// <summary>Fuso usado para decidir se é dia ou noite (ID IANA, ex.: America/Sao_Paulo).</summary>
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";

    public TimeOnly DayStartsAt { get; set; } = new(6, 0);

    public TimeOnly NightStartsAt { get; set; } = new(22, 0);

    public PeriodLimitOptions Day { get; set; } = new() { MaxAmountPerHour = 5_000m, MaxAttemptsPerHour = 5 };

    public PeriodLimitOptions Night { get; set; } = new() { MaxAmountPerHour = 1_000m, MaxAttemptsPerHour = 3 };

    /// <summary>Converte a configuração na política de domínio; lança exceção se algum valor for inválido.</summary>
    public TransferLimitPolicy ToPolicy() =>
        new(
            TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId),
            DayStartsAt,
            NightStartsAt,
            new PeriodLimit(Day.MaxAmountPerHour, Day.MaxAttemptsPerHour),
            new PeriodLimit(Night.MaxAmountPerHour, Night.MaxAttemptsPerHour));
}

public sealed class PeriodLimitOptions
{
    public decimal MaxAmountPerHour { get; set; }

    public int MaxAttemptsPerHour { get; set; }
}
