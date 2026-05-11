namespace PayderPay.Application.Dtos.External;

public class PaymentGatewayRequest
{
    public Guid DebtId { get; set; }
    public decimal Amount { get; set; }
}
