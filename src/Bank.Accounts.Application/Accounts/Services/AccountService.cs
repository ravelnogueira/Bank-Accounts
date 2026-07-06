using FluentValidation;
using Bank.Accounts.Domain.Outbox;
using Microsoft.Extensions.Logging;
using Bank.Accounts.Domain.Accounts;
using Bank.Accounts.Application.Common.Clock;
using Bank.Accounts.Application.Common.Errors;
using Bank.Accounts.Application.Accounts.DTOs;
using Bank.Accounts.Application.Common.Logging;
using Bank.Accounts.Application.Common.Validation;
using Bank.Accounts.Application.Accounts.Interfaces;

namespace Bank.Accounts.Application.Accounts.Services;

public sealed class AccountService(
    IAccountRepository accountRepository,
    IAccountCacheService cache,
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<CreateAccountRequest> createValidator,
    IValidator<UpdateAccountRequest> updateValidator,
    IValidator<AccountListQuery> listValidator,
    ILogger<AccountService> logger) : IAccountService
{
    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var normalizedTaxId = TaxIdValidator.Normalize(request.TaxId);
        var normalizedRequest = request with { TaxId = normalizedTaxId };

        await createValidator.ValidateRequestAsync(normalizedRequest, cancellationToken);

        logger.LogInformation("Creating account. HolderName={HolderName}, MaskedTaxId={MaskedTaxId}",
            normalizedRequest.HolderName, SensitiveDataMasker.MaskTaxId(normalizedRequest.TaxId));

        if (await accountRepository.ExistsByTaxIdAsync(normalizedRequest.TaxId, cancellationToken))
        {
            throw new TaxIdAlreadyExistsException();
        }

        var account = Account.Create(Guid.NewGuid(), normalizedRequest.HolderName, normalizedRequest.TaxId,
            normalizedRequest.Status ?? AccountStatus.Active, clock.UtcNow);

        await accountRepository.AddAsync(account, cancellationToken);
        await AddDomainEventsToOutboxAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Account created successfully. AccountId={AccountId}", account.Id);

        return Map(account);
    }

    public async Task<AccountResponse> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAccountByIdAsync(id, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug("Account cache hit. AccountId={AccountId}", id);
            return cached;
        }

        logger.LogDebug("Account cache miss. AccountId={AccountId}", id);
        var account = await accountRepository.GetAccountByIdAsync(id, cancellationToken)
                      ?? throw new AccountNotFoundException(id);

        var response = Map(account);
        await cache.SetAsync(response, cancellationToken);
        return response;
    }

    public async Task<PagedResponse<AccountResponse>> ListAccountAsync(AccountListQuery query,
        CancellationToken cancellationToken)
    {
        await listValidator.ValidateRequestAsync(query, cancellationToken);

        if (string.IsNullOrWhiteSpace(query.TaxId))
            return await accountRepository.ListAccountAsync(query, cancellationToken);

        var response = await GetAccountByTaxIdAsync(TaxIdValidator.Normalize(query.TaxId), cancellationToken);

        var matchesStatus = response is not null && (!query.Status.HasValue || response.Status == query.Status);

        var items = matchesStatus && query.Page == 1 ? new[] { response! } : [];

        return PagedResponse<AccountResponse>.Create(items,
            query.Page,
            query.PageSize,
            matchesStatus ? 1 : 0);
    }

    public async Task<AccountResponse> UpdateAccountAsync(Guid id, UpdateAccountRequest request, CancellationToken ct)
    {
        await updateValidator.ValidateRequestAsync(request, ct);
        logger.LogInformation("Updating account. AccountId={AccountId}", id);

        var account = await accountRepository.GetTrackedByIdAsync(id, ct)
                      ?? throw new AccountNotFoundException(id);

        account.Update(request.HolderName, request.Status, clock.UtcNow);
        await AddDomainEventsToOutboxAsync(account, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await InvalidateCacheAsync(account, ct);

        logger.LogInformation("Account updated successfully. AccountId={AccountId}", id);
        return Map(account);
    }

    public async Task DeleteAccountAsync(Guid id, CancellationToken ct)
    {
        logger.LogInformation("Deleting account. AccountId={AccountId}", id);
        var account = await accountRepository.GetTrackedByIdAsync(id, ct)
                      ?? throw new AccountNotFoundException(id);

        account.Delete(clock.UtcNow);
        await AddDomainEventsToOutboxAsync(account, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await InvalidateCacheAsync(account, ct);

        logger.LogInformation("Account deleted successfully. AccountId={AccountId}", id);
    }

    #region PRIVATE METHODS

    private async Task<AccountResponse?> GetAccountByTaxIdAsync(string taxId, CancellationToken ct)
    {
        var cached = await cache.GetByTaxIdAsync(taxId, ct);
        if (cached is not null)
        {
            return cached;
        }

        var account = await accountRepository.GetByTaxIdAsync(taxId, ct);

        if (account is null)
        {
            return null;
        }

        var response = Map(account);
        await cache.SetAsync(response, ct);
        return response;
    }

    private async Task AddDomainEventsToOutboxAsync(Account account, CancellationToken ct)
    {
        var messages = account.DequeueDomainEvents()
            .Select(domainEvent => OutboxMessage.From(
                domainEvent,
                AccountDomainEventSerializer.Serialize(domainEvent)))
            .ToArray();

        await unitOfWork.AddOutboxMessagesAsync(messages, ct);

        logger.LogInformation("Outbox messages created. AccountId={AccountId}, MessageCount={MessageCount}",
            account.Id,
            messages.Length);
    }

    private async Task InvalidateCacheAsync(Account account, CancellationToken ct)
    {
        try
        {
            await cache.RemoveAsync(account.Id, account.TaxId, ct);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Account was persisted but cache invalidation failed. AccountId={AccountId}",
                account.Id);
        }
    }

    private static AccountResponse Map(Account account) =>
        new(account.Id, account.HolderName, account.TaxId, account.Status, account.CreatedAt, account.UpdatedAt);

    #endregion
}
