namespace PayderPay.Application.DTOs.External;

public class DebtProviderQueryRequest
{
    public Guid SubscriptionId { get; set; }
    public string SubscriberNumber { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public int DueDayOfMonth { get; set; }
}
