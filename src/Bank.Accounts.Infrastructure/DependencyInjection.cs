using FluentValidation;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using Bank.Accounts.Infrastructure.Time;
using Bank.Accounts.Infrastructure.Cache;
using Microsoft.Extensions.Configuration;
using Bank.Accounts.Infrastructure.Health;
using Bank.Accounts.Infrastructure.Outbox;
using Bank.Accounts.Application.Common.Clock;
using Bank.Accounts.Infrastructure.Messaging;
using Bank.Accounts.Application.Accounts.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Bank.Accounts.Infrastructure.Persistence;
using Bank.Accounts.Application.Outbox.Services;
using Bank.Accounts.Application.Outbox.Interfaces;
using Bank.Accounts.Application.Accounts.Services;
using Bank.Accounts.Application.Accounts.Interfaces;
using Bank.Accounts.Application.Accounts.Validation;
using Bank.Accounts.Infrastructure.Persistence.Repositories;

namespace Bank.Accounts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? throw new InvalidOperationException(
                "Connection string 'PostgreSql' is required.");
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException(
                "Connection string 'Redis' is required.");

        services.AddDbContext<AppDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisConnection);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountCacheService, RedisAccountCacheService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IValidator<CreateAccountRequest>, CreateAccountRequestValidator>();
        services.AddScoped<IValidator<UpdateAccountRequest>, UpdateAccountRequestValidator>();
        services.AddScoped<IValidator<AccountListQuery>, AccountListQueryValidator>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<IOutboxStore, PostgresOutboxStore>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<OutboxProcessorService>();

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("postgresql")
            .AddCheck<RedisHealthCheck>("redis")
            .AddCheck<OutboxHealthCheck>("outbox");

        return services;
    }
}
