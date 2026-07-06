using Bank.Accounts.Domain.Outbox;

namespace Bank.Accounts.Application.Accounts.Interfaces;

public interface IUnitOfWork
{
    Task AddOutboxMessagesAsync(IReadOnlyCollection<OutboxMessage> messages, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}