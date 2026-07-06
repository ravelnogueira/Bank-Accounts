using Bank.Accounts.Application.Accounts.DTOs;

namespace Bank.Accounts.Application.Accounts.Interfaces;

public interface IAccountService
{
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken);
    Task<AccountResponse> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResponse<AccountResponse>> ListAccountAsync(AccountListQuery query, CancellationToken cancellationToken);
    Task<AccountResponse> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken ct);
    Task DeleteAccountAsync(Guid id, CancellationToken ct);
}