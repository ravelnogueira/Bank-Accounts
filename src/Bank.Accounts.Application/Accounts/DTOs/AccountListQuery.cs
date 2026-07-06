using Bank.Accounts.Domain.Accounts;

namespace Bank.Accounts.Application.Accounts.DTOs;

public sealed record AccountListQuery(string? TaxId, AccountStatus? Status, int Page = 1, int PageSize = 20);