using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.Infrastructure.Persistence.Configurations;

internal sealed class TransferAttemptConfiguration : IEntityTypeConfiguration<TransferAttempt>
{
    public void Configure(EntityTypeBuilder<TransferAttempt> builder)
    {
        builder.ToTable("TransferAttempts");

        builder.HasKey(attempt => attempt.Id);

        builder.Property(attempt => attempt.RejectionReason).HasConversion<string>().HasMaxLength(50);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(attempt => attempt.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Transfer>()
            .WithMany()
            .HasForeignKey(attempt => attempt.TransferId)
            .OnDelete(DeleteBehavior.Restrict);

        // Consulta dos limites por hora: WHERE AccountId = @id AND AttemptedAt > @windowStart,
        // com COUNT(*) e SUM(Amount) WHERE Succeeded = 1. O INCLUDE torna o índice "cobridor",
        // sem precisar voltar à tabela.
        builder.HasIndex(attempt => new { attempt.AccountId, attempt.AttemptedAt })
            .IncludeProperties(attempt => new { attempt.Amount, attempt.Succeeded });
    }
}
