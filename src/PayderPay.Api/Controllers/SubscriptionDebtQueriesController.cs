using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayderPay.Application.Services;
using PayderPay.Application.Dtos.Debts;

namespace PayderPay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/subscriptions/{subscriptionId:guid}/debt-queries")]
public class SubscriptionDebtQueriesController : ControllerBase
{
    private readonly IDebtQueryService _debtQueryService;

    public SubscriptionDebtQueriesController(IDebtQueryService debtQueryService)
    {
        _debtQueryService = debtQueryService;
    }

    [HttpPost]
    public async Task<ActionResult<DebtQueryResponse>> QueryDebt(
        Guid subscriptionId,
        [FromBody] DebtQueryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _debtQueryService.QueryAsync(subscriptionId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DebtQueryHistoryItemResponse>>> GetHistory(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var result = await _debtQueryService.GetHistoryAsync(subscriptionId, cancellationToken);
        return Ok(result);
    }
}
