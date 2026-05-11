using Microsoft.EntityFrameworkCore;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Application.Common.Pagination;
using PayderPay.Domain.Entities;
using PayderPay.Domain.Enums;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly PayderPayDbContext _context;

    public SubscriptionRepository(PayderPayDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.ProviderName)
            .ThenBy(x => x.SubscriberNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.Status == SubscriptionStatus.Active)
            .OrderBy(x => x.ProviderName)
            .ThenBy(x => x.SubscriberNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Subscription>> GetByCustomerPagedAsync(
        Guid customerId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Subscriptions
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId);

        var total = await query.CountAsync(cancellationToken);
        if (total == 0)
        {
            return PagedResult<Subscription>.Empty(page);
        }

        var items = await query
            .OrderBy(x => x.ProviderName)
            .ThenBy(x => x.SubscriberNumber)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Subscription>.From(items, total, page);
    }

    public async Task<PagedResult<Subscription>> GetActivePagedAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Subscriptions
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.Status == SubscriptionStatus.Active);

        var total = await query.CountAsync(cancellationToken);
        if (total == 0)
        {
            return PagedResult<Subscription>.Empty(page);
        }

        var items = await query
            .OrderBy(x => x.ProviderName)
            .ThenBy(x => x.SubscriberNumber)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Subscription>.From(items, total, page);
    }

    public async Task<IReadOnlyList<Subscription>> GetDueSubscriptionsAsync(DateOnly referenceDate, int leadDays, CancellationToken cancellationToken = default)
    {
        var maxDate = referenceDate.AddDays(leadDays);

        var candidates = await _context.Subscriptions
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.Status == SubscriptionStatus.Active && x.Customer.IsActive)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(subscription =>
            {
                var nextDueDate = CalculateNextDueDate(subscription.DueDayOfMonth, referenceDate);
                return nextDueDate >= referenceDate && nextDueDate <= maxDate;
            })
            .ToList();
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.Subscriptions.AddAsync(subscription, cancellationToken);
    }

    public void Update(Subscription subscription)
    {
        _context.Subscriptions.Update(subscription);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Subscriptions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.IsDeleted = true;
        entity.Status = SubscriptionStatus.Passive;
        entity.DeletedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static DateOnly CalculateNextDueDate(int dueDayOfMonth, DateOnly referenceDate)
    {
        var dayThisMonth = Math.Min(dueDayOfMonth, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month));
        var thisMonthDate = new DateOnly(referenceDate.Year, referenceDate.Month, dayThisMonth);

        if (thisMonthDate >= referenceDate)
        {
            return thisMonthDate;
        }

        var nextMonth = referenceDate.AddMonths(1);
        var dayNextMonth = Math.Min(dueDayOfMonth, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
        return new DateOnly(nextMonth.Year, nextMonth.Month, dayNextMonth);
    }
}
