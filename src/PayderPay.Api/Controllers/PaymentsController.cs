using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayderPay.Api.Extensions;
using PayderPay.Application.Common.Pagination;
using PayderPay.Application.Services;
using PayderPay.Application.Dtos.Payments;

namespace PayderPay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentHistoryItemResponse>>> GetByCustomer(
        [FromQuery] Guid? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!customerId.HasValue)
        {
            return BadRequest(new { Message = "Query parameter 'customerId' is required." });
        }

        var result = await _paymentService.GetByCustomerPagedAsync(customerId.Value, new PageRequest(page, pageSize), cancellationToken);
        Response.AddPaginationHeaders(result);
        return Ok(result.Items);
    }
}
