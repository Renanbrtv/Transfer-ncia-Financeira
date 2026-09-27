using System.Data;
using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Domain;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.Infrastructure.Persistence;

public sealed class TransfersDbContext(DbContextOptions<TransfersDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Transfer> Transfers => Set<Transfer>();

    public DbSet<TransferAttempt> TransferAttempts => Set<TransferAttempt>();

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Todo decimal do modelo é dinheiro: DECIMAL(18,2).
        configurationBuilder.Properties<decimal>().HavePrecision(Money.Precision, Money.Scale);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransfersDbContext).Assembly);
    }
}
