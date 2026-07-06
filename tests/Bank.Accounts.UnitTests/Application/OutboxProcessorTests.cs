using Bank.Accounts.Domain.Outbox;
using Bank.Accounts.Domain.Accounts;
using Bank.Accounts.Domain.Accounts.Events;
using Bank.Accounts.Application.Outbox.Services;

namespace Bank.Accounts.UnitTests.Application;

public sealed class OutboxProcessorTests
{
    private static readonly DateTime Now =
        new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Process_WithSuccessfulPublish_MarksMessageProcessed()
    {
        var message = CreateMessage();
        var batch = new FakeOutboxBatch(message);
        var publisher = new FakeEventPublisher();
        var processor = new OutboxProcessor(
            new FakeOutboxStore(batch),
            publisher,
            new FixedClock(Now),
            new CapturingLogger<OutboxProcessor>());

        var processed = await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal(OutboxMessageStatus.Processed, message.Status);
        Assert.True(batch.Saved);
        Assert.True(batch.Committed);
    }

    [Fact]
    public async Task Process_WithPublisherFailure_IncrementsRetry()
    {
        var message = CreateMessage();
        var batch = new FakeOutboxBatch(message);
        var processor = new OutboxProcessor(
            new FakeOutboxStore(batch),
            new FakeEventPublisher(new InvalidOperationException("Broker unavailable.")),
            new FixedClock(Now),
            new CapturingLogger<OutboxProcessor>());

        await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(1, message.RetryCount);
        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal("Broker unavailable.", message.Error);
    }

    [Fact]
    public async Task Process_OnFifthFailure_MarksFailedAndEmitsCriticalAlert()
    {
        var message = CreateMessage();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            message.RecordFailure("Previous failure.");
        }

        var batch = new FakeOutboxBatch(message);
        var logger = new CapturingLogger<OutboxProcessor>();
        var processor = new OutboxProcessor(
            new FakeOutboxStore(batch),
            new FakeEventPublisher(new InvalidOperationException("Broker unavailable.")),
            new FixedClock(Now),
            logger);

        await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(OutboxMessageStatus.Failed, message.Status);
        Assert.Contains(
            logger.Entries,
            entry =>
                entry.Level == Microsoft.Extensions.Logging.LogLevel.Critical &&
                entry.EventId == OutboxProcessor.TerminalFailureEvent);
    }

    private static OutboxMessage CreateMessage()
    {
        var domainEvent = new AccountCreatedEvent(
            Guid.NewGuid(),
            "Ada Lovelace",
            "52998224725",
            AccountStatus.Active,
            Now);
        return OutboxMessage.From(domainEvent, """{"accountId":"test"}""");
    }
}
