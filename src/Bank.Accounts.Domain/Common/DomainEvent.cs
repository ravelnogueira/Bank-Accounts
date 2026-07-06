namespace Bank.Accounts.Domain.Common;

public abstract record DomainEvent(DateTime OccurredAt)
{
    public abstract string EventType { get; }
}

