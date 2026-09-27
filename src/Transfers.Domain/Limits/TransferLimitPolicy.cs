namespace Transfers.Domain.Limits;

/// <summary>
/// Define qual limite vale em um dado instante.
/// O período diurno é o intervalo [DayStartsAt, NightStartsAt) no fuso configurado; o restante é noturno.
/// A janela de contagem é móvel: os 60 minutos anteriores ao instante da tentativa.
/// </summary>
public sealed class TransferLimitPolicy
{
    public static readonly TimeSpan Window = TimeSpan.FromHours(1);

    private readonly TimeZoneInfo _timeZone;
    private readonly TimeOnly _dayStartsAt;
    private readonly TimeOnly _nightStartsAt;
    private readonly PeriodLimit _dayLimit;
    private readonly PeriodLimit _nightLimit;

    public TransferLimitPolicy(
        TimeZoneInfo timeZone,
        TimeOnly dayStartsAt,
        TimeOnly nightStartsAt,
        PeriodLimit dayLimit,
        PeriodLimit nightLimit)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(dayLimit);
        ArgumentNullException.ThrowIfNull(nightLimit);

        if (dayStartsAt >= nightStartsAt)
        {
            throw new ArgumentException("O início do período diurno deve ser anterior ao início do período noturno.", nameof(dayStartsAt));
        }

        _timeZone = timeZone;
        _dayStartsAt = dayStartsAt;
        _nightStartsAt = nightStartsAt;
        _dayLimit = dayLimit;
        _nightLimit = nightLimit;
    }

    public DayPeriod GetPeriodAt(DateTimeOffset instant)
    {
        var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, _timeZone).DateTime);

        // IsBetween é inclusivo no início e exclusivo no fim: 06:00 é dia, 22:00 já é noite.
        return localTime.IsBetween(_dayStartsAt, _nightStartsAt) ? DayPeriod.Day : DayPeriod.Night;
    }

    public PeriodLimit GetLimitAt(DateTimeOffset instant) =>
        GetPeriodAt(instant) == DayPeriod.Day ? _dayLimit : _nightLimit;

    public static DateTimeOffset WindowStart(DateTimeOffset instant) => instant - Window;
}
