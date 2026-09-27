namespace Transfers.Domain.Limits;

/// <summary>Limites aplicados à conta de origem dentro de uma janela de uma hora.</summary>
public sealed record PeriodLimit
{
    public PeriodLimit(decimal maxAmountPerHour, int maxAttemptsPerHour)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAmountPerHour);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttemptsPerHour);

        MaxAmountPerHour = maxAmountPerHour;
        MaxAttemptsPerHour = maxAttemptsPerHour;
    }

    public decimal MaxAmountPerHour { get; }

    public int MaxAttemptsPerHour { get; }
}
