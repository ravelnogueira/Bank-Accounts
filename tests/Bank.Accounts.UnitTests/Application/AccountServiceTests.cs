using Bank.Accounts.Domain.Outbox;
using Bank.Accounts.Domain.Accounts;
using Bank.Accounts.Application.Accounts.DTOs;
using Bank.Accounts.Application.Common.Errors;
using Microsoft.Extensions.Logging.Abstractions;
using Bank.Accounts.Application.Accounts.Services;
using Bank.Accounts.Application.Accounts.Validation;

namespace Bank.Accounts.UnitTests.Application;

public sealed class AccountServiceTests
{
    private static readonly DateTime Now =
        new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Create_SavesAccountAndOutboxMessage()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(
            new CreateAccountRequest("Ada Lovelace", "52998224725", null),
            CancellationToken.None);

        Assert.Equal(AccountStatus.Active, result.Status);
        Assert.Single(context.Repository.Accounts);
        var message = Assert.Single(context.UnitOfWork.Messages);
        Assert.Equal("AccountCreated", message.EventType);
        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(1, context.UnitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Create_WithDuplicateTaxId_ThrowsConflict()
    {
        var context = CreateContext();
        context.Repository.Accounts.Add(CreateAccount("52998224725"));

        await Assert.ThrowsAsync<TaxIdAlreadyExistsException>(() =>
            context.Service.CreateAsync(
                new CreateAccountRequest("Grace Hopper", "52998224725", null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithFormattedTaxId_NormalizesBeforeSaving()
    {
        var context = CreateContext();

        var result = await context.Service.CreateAsync(
            new CreateAccountRequest("Ada Lovelace", "529.982.247-25", null),
            CancellationToken.None);

        Assert.Equal("52998224725", result.TaxId);
        Assert.Equal("52998224725", Assert.Single(context.Repository.Accounts).TaxId);
    }

    [Fact]
    public async Task Update_InvalidatesCacheAndCreatesOutboxMessage()
    {
        var context = CreateContext();
        var account = CreateAccount("52998224725");
        account.DequeueDomainEvents();
        context.Repository.Accounts.Add(account);

        await context.Service.UpdateAccountAsync(
            account.Id,
            new UpdateAccountRequest("Grace Hopper", AccountStatus.Inactive),
            CancellationToken.None);

        Assert.Equal(1, context.Cache.Removed);
        Assert.Equal("AccountUpdated", Assert.Single(context.UnitOfWork.Messages).EventType);
    }

    [Fact]
    public async Task Delete_InvalidatesCacheAndSoftDeletesAccount()
    {
        var context = CreateContext();
        var account = CreateAccount("52998224725");
        account.DequeueDomainEvents();
        context.Repository.Accounts.Add(account);

        await context.Service.DeleteAccountAsync(account.Id, CancellationToken.None);

        Assert.NotNull(account.DeletedAt);
        Assert.Equal(1, context.Cache.Removed);
        Assert.Equal("AccountDeleted", Assert.Single(context.UnitOfWork.Messages).EventType);
    }

    [Fact]
    public async Task GetById_WithCacheHit_DoesNotRequireRepositoryResult()
    {
        var context = CreateContext();
        var cached = new AccountResponse(
            Guid.NewGuid(),
            "Ada Lovelace",
            "52998224725",
            AccountStatus.Active,
            Now,
            null);
        await context.Cache.SetAsync(cached, CancellationToken.None);

        var result = await context.Service.GetAccountByIdAsync(
            cached.Id,
            CancellationToken.None);

        Assert.Equal(cached, result);
        Assert.Empty(context.Repository.Accounts);
    }

    [Fact]
    public async Task GetById_WithCacheMiss_LoadsAndCachesAccount()
    {
        var context = CreateContext();
        var account = CreateAccount("52998224725");
        context.Repository.Accounts.Add(account);

        var result = await context.Service.GetAccountByIdAsync(
            account.Id,
            CancellationToken.None);
        var secondResult = await context.Service.GetAccountByIdAsync(
            account.Id,
            CancellationToken.None);

        Assert.Equal(result, secondResult);
        Assert.Equal(2, context.Cache.IdReads);
    }

    [Fact]
    public async Task List_WithPageSizeAboveMaximum_ThrowsValidationError()
    {
        var context = CreateContext();

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            context.Service.ListAccountAsync(
                new AccountListQuery(null, null, 1, 101),
                CancellationToken.None));
    }

    private static TestContext CreateContext()
    {
        var repository = new FakeAccountRepository();
        var cache = new FakeAccountCache();
        var unitOfWork = new FakeUnitOfWork();
        var service = new AccountService(
            repository,
            cache,
            unitOfWork,
            new FixedClock(Now),
            new CreateAccountRequestValidator(),
            new UpdateAccountRequestValidator(),
            new AccountListQueryValidator(),
            NullLogger<AccountService>.Instance);
        return new TestContext(service, repository, cache, unitOfWork);
    }

    private static Account CreateAccount(string taxId) =>
        Account.Create(
            Guid.NewGuid(),
            "Ada Lovelace",
            taxId,
            AccountStatus.Active,
            Now);

    private sealed record TestContext(
        AccountService Service,
        FakeAccountRepository Repository,
        FakeAccountCache Cache,
        FakeUnitOfWork UnitOfWork);
}
