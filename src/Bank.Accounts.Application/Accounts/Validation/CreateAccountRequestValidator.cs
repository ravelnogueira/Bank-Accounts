using FluentValidation;
using Bank.Accounts.Domain.Accounts;
using Bank.Accounts.Application.Accounts.DTOs;

namespace Bank.Accounts.Application.Accounts.Validation;

public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(request => request.HolderName)
            .Cascade(CascadeMode.Stop)
            .Must(holderName => !string.IsNullOrWhiteSpace(holderName))
            .WithMessage("Holder name is required.")
            .Must(holderName => holderName.Trim().Length is >= 3 and <= 150)
            .WithMessage("Holder name must contain between 3 and 150 characters.")
            .OverridePropertyName("holderName");

        RuleFor(request => request.TaxId)
            .Cascade(CascadeMode.Stop)
            .Must(taxId => !string.IsNullOrWhiteSpace(taxId))
            .WithMessage("Tax ID is required.")
            .Must(TaxIdValidator.IsValid)
            .WithMessage("Tax ID is invalid.")
            .OverridePropertyName("taxId");

        RuleFor(request => request.Status)
            .Must(status => !status.HasValue || Enum.IsDefined(status.Value))
            .WithMessage("Status must be Active or Inactive.")
            .OverridePropertyName("status");
    }
}

