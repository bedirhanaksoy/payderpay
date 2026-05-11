using PayderPay.Application.Dtos.Reminders;

namespace PayderPay.Application.Common.Interfaces.Security;

public interface IReminderService
{
    Task<IReadOnlyList<SendReminderResultResponse>> SendDuePaymentRemindersAsync(DateOnly referenceDate, int leadDays = 3, CancellationToken cancellationToken = default);
}
