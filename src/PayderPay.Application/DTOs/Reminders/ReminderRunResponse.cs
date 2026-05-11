namespace PayderPay.Application.Dtos.Reminders;

public class ReminderRunResponse
{
    public InvoiceSyncRunResultResponse InvoiceSync { get; set; } = new();
    public NotificationDeliveryRunResultResponse NotificationDelivery { get; set; } = new();
}
