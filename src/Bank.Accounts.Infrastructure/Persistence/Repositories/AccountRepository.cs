using Bank.Accounts.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Bank.Accounts.Application.Accounts.DTOs;
using Bank.Accounts.Application.Accounts.Interfaces;

namespace Bank.Accounts.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(AppDbContext dbContext) : IAccountRepository
{
    public Task AddAsync(Account account, CancellationToken ct) =>
        dbContext.Accounts.AddAsync(account, ct).AsTask();

    public Task<Account?> GetAccountByIdAsync(Guid id, CancellationToken ct) =>
        dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.Id == id, ct);

    public Task<Account?> GetTrackedByIdAsync(Guid id, CancellationToken ct) =>
        dbContext.Accounts
            .SingleOrDefaultAsync(account => account.Id == id, ct);

    public Task<Account?> GetByTaxIdAsync(string taxId, CancellationToken ct) =>
        dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.TaxId == taxId, ct);

    public Task<bool> ExistsByTaxIdAsync(string taxId, CancellationToken ct) =>
        dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(account => account.TaxId == taxId, ct);

    public async Task<PagedResponse<AccountResponse>> ListAccountAsync(AccountListQuery query, CancellationToken ct)
    {
        var accounts = dbContext.Accounts.AsNoTracking();
        if (query.Status.HasValue)
        {
            accounts = accounts.Where(account => account.Status == query.Status);
        }

        var totalItems = await accounts.CountAsync(ct);
        var items = await accounts
            .OrderBy(account => account.CreatedAt)
            .ThenBy(account => account.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(account => new AccountResponse(
                account.Id,
                account.HolderName,
                account.TaxId,
                account.Status,
                account.CreatedAt,
                account.UpdatedAt))
            .ToArrayAsync(ct);

        return PagedResponse<AccountResponse>.Create(items, query.Page, query.PageSize, totalItems);
    }
}