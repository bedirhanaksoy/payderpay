namespace PayderPay.Application.Dtos.Reminders;

public class SendReminderResultResponse
{
    public Guid CustomerId { get; set; }
    public Guid SubscriptionId { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public bool Sent { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
}
