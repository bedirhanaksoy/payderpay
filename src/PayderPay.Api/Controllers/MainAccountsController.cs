using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayderPay.Application.Services;
using PayderPay.Application.Dtos.MainAccounts;

namespace PayderPay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/customers/{customerId:guid}/main-account")]
public class MainAccountsController : ControllerBase
{
    private readonly IMainAccountService _mainAccountService;

    public MainAccountsController(IMainAccountService mainAccountService)
    {
        _mainAccountService = mainAccountService;
    }

    [HttpGet]
    public async Task<ActionResult<MainAccountResponse>> GetByCustomerId(Guid customerId, CancellationToken cancellationToken)
    {
        var result = await _mainAccountService.GetByCustomerIdAsync(customerId, cancellationToken);
        return Ok(result);
    }
}
