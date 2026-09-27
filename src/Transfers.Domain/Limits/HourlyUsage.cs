namespace Transfers.Domain.Limits;

/// <summary>
/// Uso da conta de origem na janela da última hora.
/// </summary>
/// <param name="Attempts">Todas as tentativas, inclusive as rejeitadas.</param>
/// <param name="TransferredAmount">Soma apenas das transferências concluídas.</param>
public readonly record struct HourlyUsage(int Attempts, decimal TransferredAmount)
{
    public static HourlyUsage None => new(0, 0m);
}
