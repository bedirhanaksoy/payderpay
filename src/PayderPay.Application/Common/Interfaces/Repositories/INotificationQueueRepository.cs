using PayderPay.Domain.Entities;

namespace PayderPay.Application.Common.Interfaces.Repositories;

public interface INotificationQueueRepository
{
    Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(NotificationQueueItem queueItem, CancellationToken cancellationToken = default);
    Task<bool> TryEnqueueAsync(NotificationQueueItem queueItem, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationQueueItem>> GetPendingForDeliveryAsync(DateOnly today, int maxAttempts, CancellationToken cancellationToken = default);
    Task<bool> TryMarkSendingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NotificationQueueItem?> GetByIdWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);
    void Update(NotificationQueueItem queueItem);
}
