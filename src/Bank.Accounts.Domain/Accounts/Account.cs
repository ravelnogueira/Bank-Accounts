using Bank.Accounts.Domain.Accounts.Events;
using Bank.Accounts.Domain.Common;

namespace Bank.Accounts.Domain.Accounts;

public sealed class Account
{
    private readonly List<DomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }
    public string HolderName { get; private set; } = string.Empty;
    public string TaxId { get; private set; } = string.Empty;
    public AccountStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Account(Guid id, string holderName, string taxId, AccountStatus status, DateTime createdAt)
    {
        Id = id;
        HolderName = holderName;
        TaxId = taxId;
        Status = status;
        CreatedAt = createdAt;
        _domainEvents.Add(new AccountCreatedEvent(id, holderName, taxId, status, createdAt));
    }

    public static Account Create(Guid id, string holderName, string taxId, AccountStatus status, DateTime occurredAt)
    {
        var normalizedTaxId = TaxIdValidator.Normalize(taxId);

        EnsureHolderName(holderName);
        EnsureTaxId(normalizedTaxId);
        EnsureStatus(status);
        return new Account(id, holderName.Trim(), normalizedTaxId, status, occurredAt);
    }

    public void Update(string holderName, AccountStatus status, DateTime occurredAt)
    {
        EnsureNotDeleted();
        EnsureHolderName(holderName);
        EnsureStatus(status);

        HolderName = holderName.Trim();
        Status = status;
        UpdatedAt = occurredAt;
        _domainEvents.Add(new AccountUpdatedEvent(Id, HolderName, Status, occurredAt));
    }

    public void Delete(DateTime occurredAt)
    {
        EnsureNotDeleted();
        Status = AccountStatus.Inactive;
        UpdatedAt = occurredAt;
        DeletedAt = occurredAt;
        _domainEvents.Add(new AccountDeletedEvent(Id, TaxId, occurredAt));
    }

    public IReadOnlyCollection<DomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }

    private static void EnsureHolderName(string holderName)
    {
        if (string.IsNullOrWhiteSpace(holderName) ||
            holderName.Trim().Length is < 3 or > 150)
        {
            throw new DomainRuleException("Holder name must contain between 3 and 150 characters.");
        }
    }

    private static void EnsureTaxId(string taxId)
    {
        if (!TaxIdValidator.IsValid(taxId))
        {
            throw new DomainRuleException("Tax ID is invalid.");
        }
    }

    private static void EnsureStatus(AccountStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainRuleException("Account status is invalid.");
        }
    }

    private void EnsureNotDeleted()
    {
        if (DeletedAt.HasValue)
        {
            throw new DomainRuleException("A deleted account cannot be changed.");
        }
    }
}
