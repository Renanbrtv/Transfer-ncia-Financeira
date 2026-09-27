using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.Infrastructure.Persistence.Configurations;

internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("Transfers", table =>
        {
            table.HasCheckConstraint("CK_Transfers_Amount_Positive", "[Amount] > 0");
            table.HasCheckConstraint("CK_Transfers_DifferentAccounts", "[SourceAccountId] <> [DestinationAccountId]");
        });

        builder.HasKey(transfer => transfer.Id);
        builder.Property(transfer => transfer.Id).ValueGeneratedNever();

        builder.Property(transfer => transfer.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(transfer => transfer.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(transfer => transfer.FailureReason).HasConversion<string>().HasMaxLength(50);
        builder.Property(transfer => transfer.IdempotencyKey).HasMaxLength(Transfer.IdempotencyKeyMaxLength);
        builder.Property(transfer => transfer.RowVersion).IsRowVersion();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transfer => transfer.SourceAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transfer => transfer.DestinationAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Busca do worker: WHERE Status = 'Scheduled' AND ScheduledFor <= @now ORDER BY ScheduledFor.
        builder.HasIndex(transfer => new { transfer.Status, transfer.ScheduledFor });

        // Extrato por conta (enviadas e recebidas), ordenado por data.
        builder.HasIndex(transfer => new { transfer.SourceAccountId, transfer.CreatedAt });
        builder.HasIndex(transfer => new { transfer.DestinationAccountId, transfer.CreatedAt });

        builder.HasIndex(transfer => transfer.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UX_Transfers_IdempotencyKey");
    }
}
