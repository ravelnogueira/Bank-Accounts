using Bank.Accounts.Application.Common.Errors;
using FluentValidation;

namespace Bank.Accounts.Application.Common.Validation;

public static class ValidatorExtensions
{
    public static async Task ValidateRequestAsync<T>(this IValidator<T> validator, T instance,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(failure => failure.ErrorMessage)
                    .Distinct()
                    .ToArray());

        throw new RequestValidationException(errors);
    }
}

