using PayderPay.Domain.Common;

namespace PayderPay.Domain.Entities;

public class Customer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;

    public MainAccount? MainAccount { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<NotificationQueueItem> NotificationQueueItems { get; set; } = new List<NotificationQueueItem>();
    public ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
}
