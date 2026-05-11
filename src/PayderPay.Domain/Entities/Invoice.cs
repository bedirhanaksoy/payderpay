using PayderPay.Domain.Enums;

namespace PayderPay.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public DateTime FetchedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Subscription Subscription { get; set; } = null!;
    public Customer User { get; set; } = null!;
    public ICollection<NotificationQueueItem> NotificationQueueItems { get; set; } = new List<NotificationQueueItem>();
}
