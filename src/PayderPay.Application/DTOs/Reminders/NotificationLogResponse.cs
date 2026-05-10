using PayderPay.Domain.Enums;

namespace PayderPay.Application.DTOs.Reminders;

public class NotificationLogResponse
{
    public Guid Id { get; set; }
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
}
