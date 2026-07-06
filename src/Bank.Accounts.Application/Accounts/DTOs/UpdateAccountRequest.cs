using Bank.Accounts.Domain.Accounts;

namespace Bank.Accounts.Application.Accounts.DTOs;

public sealed record UpdateAccountRequest(string HolderName, AccountStatus Status);