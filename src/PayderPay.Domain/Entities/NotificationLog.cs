using PayderPay.Domain.Common;
using PayderPay.Domain.Enums;

namespace PayderPay.Domain.Entities;

public class NotificationLog : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid SubscriptionId { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? FailureReason { get; set; }

    public Customer Customer { get; set; } = null!;
    public Subscription Subscription { get; set; } = null!;
}
