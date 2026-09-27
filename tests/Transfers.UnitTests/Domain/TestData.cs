using Transfers.Domain.Accounts;
using Transfers.Domain.Limits;

namespace Transfers.UnitTests.Domain;

internal static class TestData
{
    /// <summary>15/01/2026 13:00 UTC = 10:00 em São Paulo (período diurno).</summary>
    public static readonly DateTimeOffset Now = new(2026, 1, 15, 13, 0, 0, TimeSpan.Zero);

    public static readonly PeriodLimit DayLimit = new(maxAmountPerHour: 5_000m, maxAttemptsPerHour: 5);

    public static readonly PeriodLimit NightLimit = new(maxAmountPerHour: 1_000m, maxAttemptsPerHour: 3);

    /// <summary>Fuso fixo UTC-3 criado em memória, para não depender do banco de fusos do sistema operacional.</summary>
    public static readonly TimeZoneInfo SaoPaulo =
        TimeZoneInfo.CreateCustomTimeZone("Test/Sao_Paulo", TimeSpan.FromHours(-3), "São Paulo", "BRT");

    public static Account ActiveAccount(int id, decimal balance, decimal overdraftLimit = 0m) =>
        Account.Restore(id, $"Titular {id}", balance, overdraftLimit, AccountStatus.Active);

    public static Account AccountWithStatus(int id, AccountStatus status, decimal balance = 1_000m) =>
        Account.Restore(id, $"Titular {id}", balance, overdraftLimit: 0m, status);
}
