using PayderPay.Domain.Enums;

namespace PayderPay.Domain.Entities;

public class NotificationQueueItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public Guid UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public NotificationQueueStatus Status { get; set; } = NotificationQueueStatus.Pending;
    public DateOnly ScheduledFor { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public Customer User { get; set; } = null!;
}
