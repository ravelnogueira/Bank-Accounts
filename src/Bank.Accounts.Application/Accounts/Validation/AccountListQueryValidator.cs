using FluentValidation;
using Bank.Accounts.Domain.Accounts;
using Bank.Accounts.Application.Accounts.DTOs;

namespace Bank.Accounts.Application.Accounts.Validation;

public sealed class AccountListQueryValidator : AbstractValidator<AccountListQuery>
{
    public AccountListQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than zero.")
            .OverridePropertyName("page");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.")
            .OverridePropertyName("pageSize");

        When(
            query => !string.IsNullOrWhiteSpace(query.TaxId),
            () =>
            {
                RuleFor(query => query.TaxId!)
                    .Must(TaxIdValidator.IsValid)
                    .WithMessage("Tax ID is invalid.")
                    .OverridePropertyName("taxId");
            });

        RuleFor(query => query.Status)
            .Must(status => !status.HasValue || Enum.IsDefined(status.Value))
            .WithMessage("Status must be Active or Inactive.")
            .OverridePropertyName("status");
    }
}

