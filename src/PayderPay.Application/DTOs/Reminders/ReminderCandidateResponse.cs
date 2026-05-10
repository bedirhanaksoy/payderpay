using PayderPay.Domain.Enums;

namespace PayderPay.Application.DTOs.Reminders;

public class ReminderCandidateResponse
{
    public Guid CustomerId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public SubscriptionType SubscriptionType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string SubscriberNumber { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateOnly DueDate { get; set; }
}
