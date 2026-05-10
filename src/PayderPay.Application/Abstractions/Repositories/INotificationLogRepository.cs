using PayderPay.Domain.Entities;

namespace PayderPay.Application.Abstractions.Repositories;

public interface INotificationLogRepository
{
    Task AddAsync(NotificationLog notificationLog, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationLog>> GetBySubscriptionPeriodAsync(Guid subscriptionId, int periodYear, int periodMonth, CancellationToken cancellationToken = default);
    Task<bool> HasSentReminderForPeriodAsync(Guid subscriptionId, int periodYear, int periodMonth, CancellationToken cancellationToken = default);
}
