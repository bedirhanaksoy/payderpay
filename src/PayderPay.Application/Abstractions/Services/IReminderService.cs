using PayderPay.Application.DTOs.Reminders;

namespace PayderPay.Application.Abstractions.Services;

public interface IReminderService
{
    Task<IReadOnlyList<SendReminderResultResponse>> SendDuePaymentRemindersAsync(DateOnly referenceDate, int leadDays = 3, CancellationToken cancellationToken = default);
}
