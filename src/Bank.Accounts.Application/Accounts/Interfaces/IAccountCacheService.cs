using Bank.Accounts.Application.Accounts.DTOs;

namespace Bank.Accounts.Application.Accounts.Interfaces;

public interface IAccountCacheService
{
    Task<AccountResponse?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AccountResponse?> GetByTaxIdAsync(string taxId, CancellationToken cancellationToken);
    Task SetAsync(AccountResponse account, CancellationToken cancellationToken);
    Task RemoveAsync(Guid id, string taxId, CancellationToken cancellationToken);
}
