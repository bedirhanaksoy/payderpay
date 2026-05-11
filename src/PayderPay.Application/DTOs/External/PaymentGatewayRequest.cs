namespace PayderPay.Application.Dtos.External;

public class PaymentGatewayRequest
{
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
}
