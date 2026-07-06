using Bank.Accounts.Domain.Accounts;

namespace Bank.Accounts.Application.Accounts.DTOs;

public sealed record CreateAccountRequest(string HolderName, string TaxId, AccountStatus? Status);
