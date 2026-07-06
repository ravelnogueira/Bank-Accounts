using System.Text.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using Bank.Accounts.Application.Accounts.DTOs;
using Bank.Accounts.Application.Accounts.Interfaces;

namespace Bank.Accounts.Infrastructure.Cache;

public sealed class RedisAccountCacheService(IConnectionMultiplexer connection, ILogger<RedisAccountCacheService> logger) : IAccountCacheService
{
    private static readonly TimeSpan TimeToLive = TimeSpan.FromHours(12);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task<AccountResponse?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken) =>
        GetAsync($"account:{id}", cancellationToken);

    public Task<AccountResponse?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken) =>
        GetAsync($"account:tax-id:{taxId}", cancellationToken);

    public async Task SetAsync(AccountResponse account, CancellationToken cancellationToken)
    {
        try
        {
            var database = connection.GetDatabase();
            var payload = JsonSerializer.Serialize(account, SerializerOptions);
            await Task.WhenAll(database.StringSetAsync($"account:{account.Id}", payload, TimeToLive),
                database.StringSetAsync($"account:tax-id:{account.TaxId}", payload, TimeToLive));
        }
        catch (RedisException exception)
        {
            logger.LogWarning(
                exception,
                "Redis cache write failed. AccountId={AccountId}",
                account.Id);
        }
    }

    public async Task RemoveAsync(Guid id, string taxId, CancellationToken cancellationToken)
    {
        var database = connection.GetDatabase();
        await database.KeyDeleteAsync([$"account:{id}", $"account:tax-id:{taxId}"]);
    }

    private async Task<AccountResponse?> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await connection.GetDatabase().StringGetAsync(key);
            return payload.HasValue ? JsonSerializer.Deserialize<AccountResponse>(payload.ToString(), SerializerOptions)
                : null;
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis account cache read failed.");
            return null;
        }
    }
}
