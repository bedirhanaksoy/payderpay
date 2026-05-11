using PayderPay.Domain.Enums;

namespace PayderPay.Application.Dtos.Payments;

public class PaymentHistoryItemResponse
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDateUtc { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public PaymentStatus Status { get; set; }
}
