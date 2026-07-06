using Bank.Accounts.Domain.Accounts;

namespace Bank.Accounts.Application.Common.Logging;

public static class SensitiveDataMasker
{
    public static string MaskTaxId(string? taxId)
    {
        var normalizedTaxId = TaxIdValidator.Normalize(taxId);

        return normalizedTaxId.Length != 11 ? "***" :
            $"***{normalizedTaxId.Substring(3, 6)}**";
    }
}
