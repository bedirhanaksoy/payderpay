namespace PayderPay.Application.Dtos.Reminders;

public class NotificationDeliveryRunResultResponse
{
    public int ProcessedItems { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingForRetryCount { get; set; }
    public int MaxRetryReachedCount { get; set; }
    public int SkippedClosedOrPaidCount { get; set; }
    public int ErrorCount { get; set; }
}
