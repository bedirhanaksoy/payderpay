using Microsoft.AspNetCore.Mvc;
using PayderPay.MockApi.Contracts;

namespace PayderPay.MockApi.Controllers;

[ApiController]
[Route("api/mock/debt")]
public class DebtMockController : ControllerBase
{
    [HttpPost("query")]
    public ActionResult<MockDebtQueryResponse> QueryDebt([FromBody] MockDebtQueryRequest request)
    {
        var safeDueDay = Math.Min(request.DueDayOfMonth, DateTime.DaysInMonth(request.PeriodYear, request.PeriodMonth));
        var dueDate = new DateOnly(request.PeriodYear, request.PeriodMonth, safeDueDay);

        var hash = Math.Abs(HashCode.Combine(
            request.SubscriptionId,
            request.ProviderName,
            request.SubscriberNumber,
            request.PeriodYear,
            request.PeriodMonth));

        var amount = 100m + (hash % 5000) / 10m;

        var response = new MockDebtQueryResponse
        {
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            DueDate = dueDate,
            PeriodYear = request.PeriodYear,
            PeriodMonth = request.PeriodMonth,
            ProviderRef = $"DEBT-{hash % 1000000:D6}"
        };

        return Ok(response);
    }
}
