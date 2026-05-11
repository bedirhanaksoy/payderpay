using Microsoft.EntityFrameworkCore;
using PayderPay.Application.Common.Interfaces.Repositories;
using PayderPay.Domain.Entities;
using PayderPay.Infrastructure.Persistence;

namespace PayderPay.Infrastructure.Repositories;

public class DebtQueryResultRepository : IDebtQueryResultRepository
{
    private readonly PayderPayDbContext _context;

    public DebtQueryResultRepository(PayderPayDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DebtQueryResult debtQueryResult, CancellationToken cancellationToken = default)
    {
        await _context.DebtQueryResults.AddAsync(debtQueryResult, cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyList<DebtQueryResult> debtQueryResults, CancellationToken cancellationToken = default)
    {
        if (debtQueryResults.Count == 0)
        {
            return;
        }

        await _context.DebtQueryResults.AddRangeAsync(debtQueryResults, cancellationToken);
    }

    public async Task<DebtQueryResult?> GetCurrentBySubscriptionAndDebtIdAsync(
        Guid subscriptionId,
        Guid debtId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DebtQueryResults
            .Where(x => x.SubscriptionId == subscriptionId && x.DebtId == debtId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DebtQueryResult>> GetCurrentBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.DebtQueryResults
            .AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId)
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task SoftDeleteCurrentBySubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var currentItems = await _context.DebtQueryResults
            .Where(x => x.SubscriptionId == subscriptionId)
            .ToListAsync(cancellationToken);

        if (currentItems.Count == 0)
        {
            return;
        }

        var utcNow = DateTime.UtcNow;
        foreach (var item in currentItems)
        {
            item.IsDeleted = true;
            item.DeletedAtUtc = utcNow;
            item.UpdatedAtUtc = utcNow;
        }
    }

    public void Update(DebtQueryResult debtQueryResult)
    {
        _context.DebtQueryResults.Update(debtQueryResult);
    }
}
