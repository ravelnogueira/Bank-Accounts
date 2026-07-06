using Bank.Accounts.Domain.Outbox;

namespace Bank.Accounts.Application.Outbox.Interfaces;

public interface IOutboxBatch : IAsyncDisposable
{
    IReadOnlyList<OutboxMessage> Messages { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
}
