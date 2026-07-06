using Bank.Accounts.Domain.Common;

namespace Bank.Accounts.Domain.Accounts.Events;

public sealed record AccountCreatedEvent(
    Guid AccountId,
    string HolderName,
    string TaxId,
    AccountStatus Status,
    DateTime OccurredAt) : DomainEvent(OccurredAt)
{
    public override string EventType => "AccountCreated";
}

public sealed record AccountUpdatedEvent(
    Guid AccountId,
    string HolderName,
    AccountStatus Status,
    DateTime OccurredAt) : DomainEvent(OccurredAt)
{
    public override string EventType => "AccountUpdated";
}

public sealed record AccountDeletedEvent(
    Guid AccountId,
    string TaxId,
    DateTime OccurredAt) : DomainEvent(OccurredAt)
{
    public override string EventType => "AccountDeleted";
}

