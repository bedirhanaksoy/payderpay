using Microsoft.EntityFrameworkCore;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly PayderPayDbContext _context;

    public NotificationLogRepository(PayderPayDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NotificationLog notificationLog, CancellationToken cancellationToken = default)
    {
        await _context.NotificationLogs.AddAsync(notificationLog, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationLog>> GetBySubscriptionPeriodAsync(Guid subscriptionId, int periodYear, int periodMonth, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationLogs
            .AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId && x.PeriodYear == periodYear && x.PeriodMonth == periodMonth)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasSentReminderForPeriodAsync(Guid subscriptionId, int periodYear, int periodMonth, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationLogs.AnyAsync(
            x => x.SubscriptionId == subscriptionId &&
                 x.PeriodYear == periodYear &&
                 x.PeriodMonth == periodMonth &&
                 x.Channel == NotificationChannel.Email &&
                 x.Status == NotificationStatus.Sent,
            cancellationToken);
    }
}
