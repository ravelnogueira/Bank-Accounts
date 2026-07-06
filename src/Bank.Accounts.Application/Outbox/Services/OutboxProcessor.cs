using Bank.Accounts.Application.Common.Clock;
using Bank.Accounts.Application.Outbox.Interfaces;
using Bank.Accounts.Domain.Outbox;
using Microsoft.Extensions.Logging;

namespace Bank.Accounts.Application.Outbox.Services;

public sealed class OutboxProcessor(
    IOutboxStore store,
    IEventPublisher publisher,
    IClock clock,
    ILogger<OutboxProcessor> logger)
{
    private const int BatchSize = 20;
    public static readonly EventId TerminalFailureEvent =
        new(5001, "OUTBOX_MESSAGE_FAILED");

    public async Task<int> ProcessAsync(CancellationToken cancellationToken)
    {
        await using var batch = await store.ClaimPendingAsync(BatchSize, cancellationToken);

        if (batch.Messages.Count == 0)
        {
            await batch.CommitAsync(cancellationToken);
            return 0;
        }

        logger.LogInformation(
            "Processing outbox messages. BatchSize={BatchSize}",
            batch.Messages.Count);

        foreach (var message in batch.Messages)
        {
            await PublishMessageAsync(message, cancellationToken);
        }

        await batch.SaveChangesAsync(cancellationToken);
        await batch.CommitAsync(cancellationToken);
        return batch.Messages.Count;
    }

    private async Task PublishMessageAsync(OutboxMessage message, CancellationToken ct)
    {
        try
        {
            await publisher.PublishAsync(message.Id, message.EventType, message.Payload, ct);
            message.MarkProcessed(clock.UtcNow);
            logger.LogInformation("Outbox message published. OutboxMessageId={OutboxMessageId}, EventType={EventType}",
                message.Id,
                message.EventType);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            message.RecordFailure(exception.Message);
            LogPublishFailure(message, exception);
        }
    }

    private void LogPublishFailure(OutboxMessage message, Exception exception)
    {
        if (message.Status == OutboxMessageStatus.Failed)
        {
            logger.LogCritical(
                TerminalFailureEvent,
                exception,
                "Outbox message permanently failed. OutboxMessageId={OutboxMessageId}, EventType={EventType}, RetryCount={RetryCount}",
                message.Id,
                message.EventType,
                message.RetryCount);
            return;
        }

        logger.LogWarning(
            exception,
            "Outbox message publish failed. OutboxMessageId={OutboxMessageId}, EventType={EventType}, RetryCount={RetryCount}",
            message.Id,
            message.EventType,
            message.RetryCount);
    }
}
