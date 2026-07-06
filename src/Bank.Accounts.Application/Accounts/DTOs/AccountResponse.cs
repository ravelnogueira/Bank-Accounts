using Bank.Accounts.Domain.Accounts;

namespace Bank.Accounts.Application.Accounts.DTOs;

public sealed record AccountResponse(
    Guid Id,
    string HolderName,
    string TaxId,
    AccountStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
