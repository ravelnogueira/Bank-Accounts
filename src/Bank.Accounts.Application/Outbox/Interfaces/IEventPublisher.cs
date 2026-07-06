namespace Bank.Accounts.Application.Outbox.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(Guid messageId, string eventType, string payload, CancellationToken cancellationToken);
}
