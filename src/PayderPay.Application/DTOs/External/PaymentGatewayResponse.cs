namespace PayderPay.Application.DTOs.External;

public class PaymentGatewayResponse
{
    public bool IsSuccessful { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? FailureReason { get; set; }
}
