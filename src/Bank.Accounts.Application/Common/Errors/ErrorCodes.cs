namespace Bank.Accounts.Application.Common.Errors;

public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string AccountNotFound = "ACCOUNT_NOT_FOUND";
    public const string AccountTaxIdAlreadyExists = "ACCOUNT_TAX_ID_ALREADY_EXISTS";
    public const string InvalidAccountStatus = "INVALID_ACCOUNT_STATUS";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
}

