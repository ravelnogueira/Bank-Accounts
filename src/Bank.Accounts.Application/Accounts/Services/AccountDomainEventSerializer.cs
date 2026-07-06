using System.Text.Json;
using System.Text.Json.Serialization;
using Bank.Accounts.Domain.Accounts.Events;
using Bank.Accounts.Domain.Common;

namespace Bank.Accounts.Application.Accounts.Services;

internal static class AccountDomainEventSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(DomainEvent domainEvent) =>
        domainEvent switch
        {
            AccountCreatedEvent created => JsonSerializer.Serialize(
                new
                {
                    created.AccountId,
                    created.HolderName,
                    created.TaxId,
                    created.Status,
                    created.OccurredAt
                }, SerializerOptions),
            AccountUpdatedEvent updated => JsonSerializer.Serialize(
                new
                {
                    updated.AccountId,
                    updated.HolderName,
                    updated.Status,
                    updated.OccurredAt
                },
                SerializerOptions),
            AccountDeletedEvent deleted => JsonSerializer.Serialize(
                new
                {
                    deleted.AccountId,
                    deleted.TaxId,
                    deleted.OccurredAt
                },
                SerializerOptions),
            _ => throw new InvalidOperationException(
                $"Unsupported domain event '{domainEvent.GetType().Name}'.")
        };
}
