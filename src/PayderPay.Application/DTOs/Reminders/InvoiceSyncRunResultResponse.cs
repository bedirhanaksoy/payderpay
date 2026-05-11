namespace PayderPay.Application.Dtos.Reminders;

public class InvoiceSyncRunResultResponse
{
    public int ProcessedSubscriptions { get; set; }
    public int SucceededSubscriptions { get; set; }
    public int FailedSubscriptions { get; set; }
    public int CreatedInvoices { get; set; }
    public int UpdatedInvoices { get; set; }
    public int MarkedPaidInvoices { get; set; }
    public int QueuedNotifications { get; set; }
    public int ErrorCount { get; set; }
    public bool ShortCircuited { get; set; }
}
