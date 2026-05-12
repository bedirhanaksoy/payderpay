using PayderPay.Domain.Common;
using PayderPay.Domain.Enums;

namespace PayderPay.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid CustomerId { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string SubscriberNumber { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public int DueDayOfMonth { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Debt> Debts { get; set; } = new List<Debt>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
}
