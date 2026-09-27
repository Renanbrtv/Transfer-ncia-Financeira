using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transfers.Domain.Accounts;

namespace Transfers.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts", table =>
        {
            // Defesa em profundidade: mesmo que algum código ignore o domínio, o banco não aceita
            // saldo abaixo do cheque especial.
            table.HasCheckConstraint("CK_Accounts_OverdraftLimit_NonNegative", "[OverdraftLimit] >= 0");
            table.HasCheckConstraint("CK_Accounts_Balance_WithinOverdraft", "[Balance] >= -[OverdraftLimit]");
        });

        builder.HasKey(account => account.Id);

        builder.Property(account => account.HolderName)
            .HasMaxLength(Account.HolderNameMaxLength)
            .IsRequired();

        builder.Property(account => account.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(account => account.RowVersion)
            .IsRowVersion();

        builder.Ignore(account => account.AvailableBalance);
        builder.Ignore(account => account.IsActive);

        builder.HasData(AccountSeed.Accounts);
    }
}
