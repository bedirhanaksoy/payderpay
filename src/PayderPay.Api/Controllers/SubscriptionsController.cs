using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayderPay.Application.Services;
using PayderPay.Application.Dtos.Subscriptions;
using PayderPay.Application.Dtos.Summaries;

namespace PayderPay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISummaryService _summaryService;

    public SubscriptionsController(ISubscriptionService subscriptionService, ISummaryService summaryService)
    {
        _subscriptionService = subscriptionService;
        _summaryService = summaryService;
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionResponse>> Create([FromBody] CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.CreateAsync(request, cancellationToken);
        return Created($"/api/subscriptions/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubscriptionResponse>> Update(Guid id, [FromBody] UpdateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _subscriptionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriptionResponse>>> GetByCustomer([FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        if (!customerId.HasValue)
        {
            return BadRequest(new { Message = "Query parameter 'customerId' is required." });
        }

        var result = await _subscriptionService.GetByCustomerAsync(customerId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<SubscriptionResponse>>> GetActive(CancellationToken cancellationToken)
    {
        var result = await _subscriptionService.GetActiveAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("unpaid")]
    public async Task<ActionResult<IReadOnlyList<UnpaidSubscriptionResponse>>> GetUnpaid(
        [FromQuery] Guid? customerId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken)
    {
        if (!customerId.HasValue)
        {
            return BadRequest(new { Message = "Query parameter 'customerId' is required." });
        }

        var result = await _summaryService.GetUnpaidSubscriptionsAsync(customerId.Value, year, month, cancellationToken);
        return Ok(result);
    }
}
