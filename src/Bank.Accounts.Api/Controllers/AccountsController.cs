using Microsoft.AspNetCore.Mvc;
using Bank.Accounts.Domain.Accounts;
using Bank.Accounts.Application.Common.Errors;
using Bank.Accounts.Application.Accounts.DTOs;
using Bank.Accounts.Application.Accounts.Interfaces;

namespace Bank.Accounts.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Produces("application/json")]
public sealed class AccountsController(IAccountService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpGet("{id:guid}", Name = "GetAccountById")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetAccountByIdAsync(id, cancellationToken));

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<AccountResponse>>> List(
        [FromQuery] string? taxId, [FromQuery] string? status, [FromQuery] int? page, [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        AccountStatus? parsedStatus = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<AccountStatus>(status, ignoreCase: true, out var value) ||
                !Enum.IsDefined(value))
            {
                throw new RequestValidationException(new Dictionary<string, string[]>
                {
                    ["status"] = ["Status must be Active or Inactive."]
                });
            }

            parsedStatus = value;
        }

        var query = new AccountListQuery(taxId, parsedStatus, page ?? 1, pageSize ?? 20);
        return Ok(await service.ListAccountAsync(query, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> Update(Guid id, UpdateAccountRequest request,
        CancellationToken cancellationToken) => Ok(await service.UpdateAccountAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAccountAsync(id, cancellationToken);
        return NoContent();
    }
}
