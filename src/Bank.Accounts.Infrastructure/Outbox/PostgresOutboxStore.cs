using System.Data;
using Bank.Accounts.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Bank.Accounts.Infrastructure.Persistence;
using Bank.Accounts.Application.Outbox.Interfaces;

namespace Bank.Accounts.Infrastructure.Outbox;

public sealed class PostgresOutboxStore(AppDbContext dbContext) : IOutboxStore
{
    public async Task<IOutboxBatch> ClaimPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            var messages = await dbContext.OutboxMessages
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM "OutboxMessages"
                    WHERE "Status" = {(int)OutboxMessageStatus.Pending}
                    ORDER BY "OccurredAt"
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(cancellationToken);

            return new PostgresOutboxBatch(dbContext, transaction, messages);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private sealed class PostgresOutboxBatch(AppDbContext dbContext, IDbContextTransaction transaction,
        IReadOnlyList<OutboxMessage> messages) : IOutboxBatch
    {
        public IReadOnlyList<OutboxMessage> Messages { get; } = messages;

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            dbContext.SaveChangesAsync(cancellationToken);

        public Task CommitAsync(CancellationToken cancellationToken) =>
            transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
