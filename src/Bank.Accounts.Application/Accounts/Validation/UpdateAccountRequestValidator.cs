using FluentValidation;
using Bank.Accounts.Application.Accounts.DTOs;

namespace Bank.Accounts.Application.Accounts.Validation;

public sealed class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(request => request.HolderName)
            .Cascade(CascadeMode.Stop)
            .Must(holderName => !string.IsNullOrWhiteSpace(holderName))
            .WithMessage("Holder name is required.")
            .Must(holderName => holderName.Trim().Length is >= 3 and <= 150)
            .WithMessage("Holder name must contain between 3 and 150 characters.")
            .OverridePropertyName("holderName");

        RuleFor(request => request.Status)
            .Must(Enum.IsDefined)
            .WithMessage("Status must be Active or Inactive.")
            .OverridePropertyName("status");
    }
}

