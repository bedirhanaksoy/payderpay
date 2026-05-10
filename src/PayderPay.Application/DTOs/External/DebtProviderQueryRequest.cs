namespace PayderPay.Application.DTOs.External;

public class DebtProviderQueryRequest
{
    public Guid SubscriptionId { get; set; }
    public string SubscriberNumber { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
}
