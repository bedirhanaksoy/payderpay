using PayderPay.Domain.Common;
using PayderPay.Domain.Enums;

namespace PayderPay.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDateUtc { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? FailureReason { get; set; }

    public Subscription Subscription { get; set; } = null!;
}
