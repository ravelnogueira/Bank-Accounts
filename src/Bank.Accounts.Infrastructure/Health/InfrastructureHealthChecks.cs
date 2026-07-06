using StackExchange.Redis;
using Bank.Accounts.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Bank.Accounts.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bank.Accounts.Infrastructure.Health;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("PostgreSQL is available.")
            : HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
    }
}

public sealed class RedisHealthCheck(IConnectionMultiplexer connection)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await connection.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy("Redis is available.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.", exception);
        }
    }
}

public sealed class OutboxHealthCheck(IServiceScopeFactory scopeFactory)
    : IHealthCheck
{
    private const int PendingBacklogThreshold = 100;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var failed = await dbContext.OutboxMessages
            .AsNoTracking()
            .CountAsync(
                message => message.Status == OutboxMessageStatus.Failed,
                cancellationToken);
        var pending = await dbContext.OutboxMessages
            .AsNoTracking()
            .CountAsync(
                message => message.Status == OutboxMessageStatus.Pending,
                cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["failed"] = failed,
            ["pending"] = pending,
            ["pendingThreshold"] = PendingBacklogThreshold
        };

        return failed > 0 || pending > PendingBacklogThreshold
            ? HealthCheckResult.Degraded("Outbox requires attention.", data: data)
            : HealthCheckResult.Healthy("Outbox is operating normally.", data);
    }
}

