namespace Bank.Accounts.Application.Common.Errors;

public abstract class ApplicationExceptionBase(string message, string code) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : ApplicationExceptionBase("One or more validation errors occurred.", ErrorCodes.ValidationError)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class AccountNotFoundException(Guid accountId)
    : ApplicationExceptionBase($"Account '{accountId}' was not found.", ErrorCodes.AccountNotFound);

public sealed class TaxIdAlreadyExistsException()
    : ApplicationExceptionBase("An account with this Tax ID already exists.", ErrorCodes.AccountTaxIdAlreadyExists);

