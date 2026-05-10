using Microsoft.AspNetCore.Mvc;
using PayderPay.Application.Abstractions.ApplicationServices;
using PayderPay.Application.DTOs.Payments;

namespace PayderPay.Api.Controllers;

[ApiController]
[Route("api/subscriptions/{subscriptionId:guid}/payments")]
public class SubscriptionPaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public SubscriptionPaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> Create(
        Guid subscriptionId,
        [FromBody] CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.CreateAsync(subscriptionId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentHistoryItemResponse>>> GetBySubscription(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetBySubscriptionAsync(subscriptionId, cancellationToken);
        return Ok(result);
    }
}
