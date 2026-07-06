using Bank.Accounts.Application.Accounts.DTOs;
using Bank.Accounts.Application.Common.Logging;
using Bank.Accounts.Application.Accounts.Validation;
using FluentValidation;

namespace Bank.Accounts.UnitTests.Application;

public sealed class ValidationAndMaskingTests
{
    [Fact]
    public async Task CreateRequest_WithEmptyName_ReturnsFieldError()
    {
        var result = await new CreateAccountRequestValidator().ValidateAsync(
            new CreateAccountRequest("", "52998224725", null));

        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == "holderName");
    }

    [Fact]
    public void MaskTaxId_DoesNotExposeCompleteTaxId()
    {
        var masked = SensitiveDataMasker.MaskTaxId("52998224725");

        Assert.Equal("***982247**", masked);
        Assert.DoesNotContain("52998224725", masked);
    }

    [Fact]
    public void MaskTaxId_WithFormattedTaxId_MasksNormalizedDigits()
    {
        var masked = SensitiveDataMasker.MaskTaxId("529.982.247-25");

        Assert.Equal("***982247**", masked);
    }

    [Fact]
    public async Task CreateRequest_WithFormattedTaxId_IsValid()
    {
        var result = await new CreateAccountRequestValidator().ValidateAsync(
            new CreateAccountRequest("Ada Lovelace", "529.982.247-25", null));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("11111111111")]
    [InlineData("52998224724")]
    public async Task CreateRequest_WithInvalidTaxId_ReturnsFieldError(string taxId)
    {
        var result = await new CreateAccountRequestValidator().ValidateAsync(
            new CreateAccountRequest("Ada Lovelace", taxId, null));

        Assert.Contains(
            result.Errors,
            failure => failure.PropertyName == "taxId");
    }
}
