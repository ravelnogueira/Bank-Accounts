using Bank.Accounts.Domain.Common;
using Bank.Accounts.Domain.Accounts;
using Bank.Accounts.Domain.Accounts.Events;

namespace Bank.Accounts.UnitTests.Domain;

public sealed class AccountTests
{
    private static readonly DateTime Now =
        new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_CreatesActiveAccountAndEvent()
    {
        var account = Account.Create(
            Guid.NewGuid(),
            "Ada Lovelace",
            "52998224725",
            AccountStatus.Active,
            Now);

        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Single(account.DomainEvents);
        Assert.IsType<AccountCreatedEvent>(account.DomainEvents.Single());
    }

    [Fact]
    public void Create_WithFormattedTaxId_StoresOnlyDigits()
    {
        var account = Account.Create(
            Guid.NewGuid(),
            "Ada Lovelace",
            "529.982.247-25",
            AccountStatus.Active,
            Now);

        Assert.Equal("52998224725", account.TaxId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("1234567890A")]
    [InlineData("11111111111")]
    [InlineData("52998224724")]
    public void Create_WithInvalidTaxId_Throws(string taxId)
    {
        Assert.Throws<DomainRuleException>(() =>
            Account.Create(
                Guid.NewGuid(),
                "Ada Lovelace",
                taxId,
                AccountStatus.Active,
                Now));
    }

    [Fact]
    public void Update_ChangesAllowedFieldsAndCreatesEvent()
    {
        var account = CreateAccount();
        account.DequeueDomainEvents();

        account.Update("Grace Hopper", AccountStatus.Inactive, Now.AddMinutes(1));

        Assert.Equal("Grace Hopper", account.HolderName);
        Assert.Equal(AccountStatus.Inactive, account.Status);
        Assert.IsType<AccountUpdatedEvent>(account.DomainEvents.Single());
    }

    [Fact]
    public void Delete_PerformsSoftDeleteAndCreatesEvent()
    {
        var account = CreateAccount();
        account.DequeueDomainEvents();

        account.Delete(Now.AddMinutes(1));

        Assert.Equal(AccountStatus.Inactive, account.Status);
        Assert.NotNull(account.DeletedAt);
        Assert.IsType<AccountDeletedEvent>(account.DomainEvents.Single());
    }

    private static Account CreateAccount() =>
        Account.Create(
            Guid.NewGuid(),
            "Ada Lovelace",
            "52998224725",
            AccountStatus.Active,
            Now);
}

