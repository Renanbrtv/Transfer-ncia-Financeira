using Transfers.Domain.Exceptions;

namespace Transfers.Domain;

/// <summary>
/// Regras de valor monetário. Os valores são armazenados como DECIMAL(18,2), então aceitamos
/// no máximo duas casas decimais e 16 dígitos inteiros.
/// </summary>
public static class Money
{
    public const int Precision = 18;
    public const int Scale = 2;
    public const decimal MaxValue = 9_999_999_999_999_999.99m;

    public static void EnsureValidAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainValidationException("amount.not_positive", "O valor deve ser maior que zero.");
        }

        if (decimal.Round(amount, Scale) != amount)
        {
            throw new DomainValidationException("amount.invalid_scale", "O valor deve ter no máximo duas casas decimais.");
        }

        if (amount > MaxValue)
        {
            throw new DomainValidationException("amount.too_large", "O valor excede o máximo suportado.");
        }
    }
}
