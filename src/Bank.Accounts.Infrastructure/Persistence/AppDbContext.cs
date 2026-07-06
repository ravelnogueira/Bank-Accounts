using Bank.Accounts.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Bank.Accounts.Domain.Accounts;

namespace Bank.Accounts.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

