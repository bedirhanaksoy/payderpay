using PayderPay.Domain.Enums;

namespace PayderPay.Application.DTOs.Subscriptions;

public class SubscriptionResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string SubscriberNumber { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public int DueDayOfMonth { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
