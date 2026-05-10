using Microsoft.AspNetCore.Mvc;
using PayderPay.Application.Abstractions.ApplicationServices;
using PayderPay.Application.DTOs.Payments;

namespace PayderPay.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentHistoryItemResponse>>> GetByCustomer([FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        if (!customerId.HasValue)
        {
            return BadRequest(new { Message = "Query parameter 'customerId' is required." });
        }

        var result = await _paymentService.GetByCustomerAsync(customerId.Value, cancellationToken);
        return Ok(result);
    }
}
