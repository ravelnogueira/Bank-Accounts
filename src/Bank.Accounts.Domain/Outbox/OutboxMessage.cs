using Bank.Accounts.Domain.Common;

namespace Bank.Accounts.Domain.Outbox;

public sealed class OutboxMessage
{
    public const int MaximumRetryCount = 5;

    private OutboxMessage()
    {
    }

    private OutboxMessage(
        Guid id,
        string eventType,
        string payload,
        DateTime occurredAt)
    {
        Id = id;
        EventType = eventType;
        Payload = payload;
        OccurredAt = occurredAt;
        Status = OutboxMessageStatus.Pending;
    }

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public OutboxMessageStatus Status { get; private set; }

    public static OutboxMessage From(DomainEvent domainEvent, string payload)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Outbox payload is required.", nameof(payload));
        }

        return new OutboxMessage(
            Guid.NewGuid(),
            domainEvent.EventType,
            payload,
            domainEvent.OccurredAt);
    }

    public void MarkProcessed(DateTime processedAt)
    {
        if (Status != OutboxMessageStatus.Pending)
        {
            throw new DomainRuleException("Only pending outbox messages can be processed.");
        }

        Status = OutboxMessageStatus.Processed;
        ProcessedAt = processedAt;
        Error = null;
    }

    public void RecordFailure(string error)
    {
        if (Status != OutboxMessageStatus.Pending)
        {
            throw new DomainRuleException("Only pending outbox messages can record a failure.");
        }

        RetryCount++;
        Error = string.IsNullOrWhiteSpace(error)
            ? "Unknown publisher error."
            : error[..Math.Min(error.Length, 4000)];

        if (RetryCount >= MaximumRetryCount)
        {
            Status = OutboxMessageStatus.Failed;
        }
    }
}

