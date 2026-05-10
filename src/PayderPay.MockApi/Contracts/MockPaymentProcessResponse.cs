namespace PayderPay.MockApi.Contracts;

public class MockPaymentProcessResponse
{
    public bool IsSuccessful { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? FailureReason { get; set; }
}
