namespace Bank.Accounts.Domain.Accounts;

public static class TaxIdValidator
{
    public static string Normalize(string? taxId) =>
        taxId is null
            ? string.Empty
            : new string(taxId.Where(char.IsAsciiDigit).ToArray());

    public static bool IsValid(string? taxId)
    {
        var normalizedTaxId = Normalize(taxId);

        if (normalizedTaxId.Length != 11 ||
            normalizedTaxId.All(character => character == normalizedTaxId[0]))
        {
            return false;
        }

        return CalculateDigit(normalizedTaxId, 9, 10) == normalizedTaxId[9] - '0' &&
               CalculateDigit(normalizedTaxId, 10, 11) == normalizedTaxId[10] - '0';
    }

    private static int CalculateDigit(string taxId, int length, int initialWeight)
    {
        var sum = 0;
        for (var index = 0; index < length; index++)
        {
            sum += (taxId[index] - '0') * (initialWeight - index);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
