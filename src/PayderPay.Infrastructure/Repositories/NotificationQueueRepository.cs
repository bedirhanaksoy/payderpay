using Microsoft.EntityFrameworkCore;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class NotificationQueueRepository : INotificationQueueRepository
{
    private readonly PayderPayDbContext _context;

    public NotificationQueueRepository(PayderPayDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationQueueItems
            .AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddAsync(NotificationQueueItem queueItem, CancellationToken cancellationToken = default)
    {
        await _context.NotificationQueueItems.AddAsync(queueItem, cancellationToken);
    }

    public async Task<bool> TryEnqueueAsync(NotificationQueueItem queueItem, CancellationToken cancellationToken = default)
    {
        var affected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO notification_queue
            (id, invoice_id, user_id, notification_type, idempotency_key, status, scheduled_for, attempts, last_error, sent_at, created_at)
            VALUES
            ({queueItem.Id}, {queueItem.InvoiceId}, {queueItem.UserId}, {queueItem.NotificationType}, {queueItem.IdempotencyKey}, {(NotificationQueueStatus.Pending).ToString()}, {queueItem.ScheduledFor}, {queueItem.Attempts}, {queueItem.LastError}, {queueItem.SentAt}, {queueItem.CreatedAt})
            ON CONFLICT (idempotency_key) DO NOTHING;", cancellationToken);

        return affected == 1;
    }

    public async Task<IReadOnlyList<NotificationQueueItem>> GetPendingForDeliveryAsync(
        DateOnly today,
        int maxAttempts,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationQueueItems
            .AsNoTracking()
            .Where(x =>
                x.Status == NotificationQueueStatus.Pending &&
                x.ScheduledFor <= today &&
                x.Attempts < maxAttempts)
            .OrderBy(x => x.ScheduledFor)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryMarkSendingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var affected = await _context.NotificationQueueItems
            .Where(x => x.Id == id && x.Status == NotificationQueueStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, NotificationQueueStatus.Sending), cancellationToken);

        return affected == 1;
    }

    public async Task<NotificationQueueItem?> GetByIdWithRelationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationQueueItems
            .Include(x => x.Invoice)
            .ThenInclude(x => x.Subscription)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<int> DeleteCompletedByTypeBeforeDateAsync(
        string notificationType,
        DateOnly beforeDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationQueueItems
            .Where(x =>
                x.NotificationType == notificationType &&
                x.ScheduledFor < beforeDate &&
                (x.Status == NotificationQueueStatus.Sent || x.Status == NotificationQueueStatus.Failed))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public void Update(NotificationQueueItem queueItem)
    {
        _context.NotificationQueueItems.Update(queueItem);
    }
}
