using Microsoft.AspNetCore.Mvc;
using PayderPay.MockApi.Contracts;

namespace PayderPay.MockApi.Controllers;

[ApiController]
[Route("api/mock/payment")]
public class PaymentMockController : ControllerBase
{
    [HttpPost("process")]
    public ActionResult<MockPaymentProcessResponse> ProcessPayment([FromBody] MockPaymentProcessRequest request)
    {
        var hash = Math.Abs(HashCode.Combine(
            request.SubscriptionId,
            request.Amount,
            request.PeriodYear,
            request.PeriodMonth,
            request.ProviderRef));

        var success = hash % 10 < 8;

        if (success)
        {
            return Ok(new MockPaymentProcessResponse
            {
                IsSuccessful = true,
                ExternalTransactionId = $"PAY-{Guid.NewGuid():N}"
            });
        }

        return Ok(new MockPaymentProcessResponse
        {
            IsSuccessful = false,
            FailureReason = "Mock gateway rejected this transaction."
        });
    }
}
