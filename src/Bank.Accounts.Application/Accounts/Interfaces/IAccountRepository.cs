using Bank.Accounts.Domain.Accounts;
using Bank.Accounts.Application.Accounts.DTOs;

namespace Bank.Accounts.Application.Accounts.Interfaces;

public interface IAccountRepository
{
    Task AddAsync(Account account, CancellationToken ct);
    Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken ct);
    Task<Account?> GetTrackedByIdAsync(Guid id, CancellationToken ct);
    Task<Account?> GetByTaxIdAsync(string taxId, CancellationToken ct);
    Task<bool> ExistsByTaxIdAsync(string taxId, CancellationToken ct);
    Task<PagedResponse<AccountResponse>> ListAccountAsync(AccountListQuery query, CancellationToken ct);
}
