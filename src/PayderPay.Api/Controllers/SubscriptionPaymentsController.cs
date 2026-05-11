using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayderPay.Api.Extensions;
using PayderPay.Application.Common.Pagination;
using PayderPay.Application.Services;
using PayderPay.Application.Dtos.Payments;

namespace PayderPay.Api.Controllers;

[ApiController]
[Authorize]
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
    public async Task<ActionResult<IReadOnlyList<PaymentHistoryItemResponse>>> GetBySubscription(
        Guid subscriptionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _paymentService.GetBySubscriptionPagedAsync(subscriptionId, new PageRequest(page, pageSize), cancellationToken);
        Response.AddPaginationHeaders(result);
        return Ok(result.Items);
    }
}
