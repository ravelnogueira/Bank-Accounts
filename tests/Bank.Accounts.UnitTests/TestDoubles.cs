using Bank.Accounts.Domain.Outbox;
using Microsoft.Extensions.Logging;
using Bank.Accounts.Domain.Accounts;
using Bank.Accounts.Application.Common.Clock;
using Bank.Accounts.Application.Accounts.DTOs;
using Bank.Accounts.Application.Outbox.Interfaces;
using Bank.Accounts.Application.Accounts.Interfaces;

namespace Bank.Accounts.UnitTests;

internal sealed class FixedClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; set; } = utcNow;
}

internal sealed class FakeAccountRepository : IAccountRepository
{
    public List<Account> Accounts { get; } = [];

    public Task AddAsync(Account account, CancellationToken ct)
    {
        Accounts.Add(account);
        return Task.CompletedTask;
    }

    public Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Accounts.SingleOrDefault(account =>
            account.Id == id && account.DeletedAt is null));

    public Task<Account?> GetTrackedByIdAsync(Guid id, CancellationToken ct) =>
        GetAccountByIdAsync(id, ct);

    public Task<Account?> GetByTaxIdAsync(string taxId, CancellationToken ct) =>
        Task.FromResult(Accounts.SingleOrDefault(account =>
            account.TaxId == taxId && account.DeletedAt is null));

    public Task<bool> ExistsByTaxIdAsync(string taxId, CancellationToken ct) =>
        Task.FromResult(Accounts.Any(account =>
            account.TaxId == taxId && account.DeletedAt is null));

    public Task<PagedResponse<AccountResponse>> ListAccountAsync(
        AccountListQuery query,
        CancellationToken ct)
    {
        var filtered = Accounts
            .Where(account => account.DeletedAt is null)
            .Where(account => !query.Status.HasValue || account.Status == query.Status)
            .ToArray();
        var items = filtered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(account => new AccountResponse(
                account.Id,
                account.HolderName,
                account.TaxId,
                account.Status,
                account.CreatedAt,
                account.UpdatedAt))
            .ToArray();

        return Task.FromResult(
            PagedResponse<AccountResponse>.Create(
                items,
                query.Page,
                query.PageSize,
                filtered.Length));
    }
}

internal sealed class FakeAccountCache : IAccountCacheService
{
    private readonly Dictionary<Guid, AccountResponse> _byId = [];
    private readonly Dictionary<string, AccountResponse> _byTaxId = [];

    public int IdReads { get; private set; }
    public int Removed { get; private set; }

    public Task<AccountResponse?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        IdReads++;
        return Task.FromResult(_byId.GetValueOrDefault(id));
    }

    public Task<AccountResponse?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken) =>
        Task.FromResult(_byTaxId.GetValueOrDefault(taxId));

    public Task SetAsync(AccountResponse account, CancellationToken cancellationToken)
    {
        _byId[account.Id] = account;
        _byTaxId[account.TaxId] = account;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, string taxId, CancellationToken cancellationToken)
    {
        Removed++;
        _byId.Remove(id);
        _byTaxId.Remove(taxId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public List<OutboxMessage> Messages { get; } = [];
    public int SaveCalls { get; private set; }

    public Task AddOutboxMessagesAsync(
        IReadOnlyCollection<OutboxMessage> messages,
        CancellationToken cancellationToken)
    {
        Messages.AddRange(messages);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeOutboxBatch(params OutboxMessage[] messages)
    : IOutboxBatch
{
    public IReadOnlyList<OutboxMessage> Messages { get; } = messages;
    public bool Saved { get; private set; }
    public bool Committed { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saved = true;
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        Committed = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class FakeOutboxStore(FakeOutboxBatch batch) : IOutboxStore
{
    public Task<IOutboxBatch> ClaimPendingAsync(
        int batchSize,
        CancellationToken cancellationToken) =>
        Task.FromResult<IOutboxBatch>(batch);
}

internal sealed class FakeEventPublisher(Exception? exception = null)
    : IEventPublisher
{
    public int Calls { get; private set; }

    public Task PublishAsync(
        Guid messageId,
        string eventType,
        string payload,
        CancellationToken cancellationToken)
    {
        Calls++;
        return exception is null
            ? Task.CompletedTask
            : Task.FromException(exception);
    }
}

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, EventId EventId)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, eventId));
}
