using PayderPay.Application.Dtos.Reminders;

namespace PayderPay.Application.Common.Interfaces.Security;

public interface IReminderService
{
    Task<IReadOnlyList<SendReminderResultResponse>> SendDuePaymentRemindersAsync(DateOnly referenceDate, int leadDays = 3, CancellationToken cancellationToken = default);
    Task<InvoiceSyncRunResultResponse> RunInvoiceSyncAsync(DateOnly referenceDate, int leadDays = 3, CancellationToken cancellationToken = default);
    Task<NotificationDeliveryRunResultResponse> RunNotificationDeliveryAsync(DateOnly referenceDate, int maxAttempts = 3, CancellationToken cancellationToken = default);
    Task<ReminderRunResponse> RunInvoiceSyncAndDeliveryAsync(DateOnly referenceDate, int leadDays = 3, int maxAttempts = 3, CancellationToken cancellationToken = default);
}
